namespace AdventurersGuild.Application.Quests;

using System;
using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Application.Adventurers;
using AdventurersGuild.Core;
using AdventurersGuild.Data.Adventurer;
using AdventurersGuild.Data.Balance;
using AdventurersGuild.Data.Quest;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Common;
using AdventurersGuild.Domain.Quests;

public class QuestFlow
{
    private const int BoardCapacity = 4;

    private readonly QuestRegistry registry;
    private readonly QuestFactory factory;
    private readonly QuestResolver resolver;
    private readonly GameClock clock;
    private readonly GuildBank bank;

    public event Action<QuestModel> Added;
    public event Action<QuestModel> Expired;
    public event Action<QuestModel> Started;
    public event Action<QuestModel> Completed;

    private readonly Random woundRng;

    public QuestFlow(QuestRegistry registry, QuestFactory factory, QuestResolver resolver, GameClock clock, GuildBank bank, int? seed = null)
    {
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        this.bank = bank ?? throw new ArgumentNullException(nameof(bank));

        woundRng = seed.HasValue
            ? new Random(unchecked(seed.Value * 7 + 3))
            : new Random();
    }

    public void Start()
    {
        clock.TimeAdvanced += OnTimeAdvanced;
        RefreshBoard();
    }

    public void Stop()
    {
        clock.TimeAdvanced -= OnTimeAdvanced;
    }

    // === Внешний API ===

    public bool StartQuest(QuestModel quest, IReadOnlyList<AdventurerModel> party)
    {
        if (quest == null) return false;
        if (quest.Status != QuestStatus.Available) return false;
        if (party == null || party.Count == 0) return false;

        var seen = new HashSet<string>();

        foreach (var a in party)
        {
            if (a == null) return false;
            if (!seen.Add(a.Id)) return false;
            if (a.HasStatus(StatusIds.Dead)) return false;
        }

        quest.Status = QuestStatus.Pending;
        registry.TakeFromBoard(quest);
        quest.Party = new List<AdventurerModel>(party);
        quest.StartTime = GameTime.NextDayStart(clock.Now);
        quest.LastProcessedAt = quest.StartTime;
        quest.CurrentRoleId = quest.StartRoleId;
        quest.AmbientIndex = 0;
        quest.DaysSpent = 0;
        quest.MinutesUsedToday = 0;
        quest.PhaseResults = new List<PhaseResult>();
        quest.CurrentPhaseEndTime = null;
        quest.IsResting = false;
        quest.RestEndTime = null;

        foreach (var a in quest.Party)
            a.SetStatus(StatusIds.Waiting);

        LogStart(quest, party);
        Started?.Invoke(quest);
        return true;
    }

    // === Обработка времени ===

    private void OnTimeAdvanced(GameTime now)
    {
        ExpireOld(now);
        ProcessInProgress(now);
        RefreshBoard();

        if (now.Minute == 0 && now.Hour == 0)
            registry.TrimHistory(now, maxAgeDays: 30);
    }

    private void ExpireOld(GameTime now)
    {
        var snapshot = new List<QuestModel>(registry.Board);

        foreach (var quest in snapshot)
        {
            if (now < quest.ExpiresAt) continue;

            quest.Status = QuestStatus.Expired;
            registry.ExpireToHistory(quest);
            Expired?.Invoke(quest);
        }
    }

    private void ProcessInProgress(GameTime now)
    {
        for (int i = 0; i < registry.Active.Count; i++)
        {
            var quest = registry.Active[i];
            ProcessQuest(quest, now);

            if (i >= registry.Active.Count) break;
            if (registry.Active[i] != quest) i--;
        }
    }

    // === Основной цикл квеста ===

    private void ProcessQuest(QuestModel quest, GameTime now)
    {
        // 1. Отправление
        if (quest.Status == QuestStatus.Pending)
        {
            if (now < quest.StartTime) return;
            quest.Status = QuestStatus.InProgress;
            foreach (var a in quest.Party)
                a.SetStatus(StatusIds.OnQuest);
            LogDepart(quest);
        }

        // 2. Лимит дней
        if (quest.DaysSpent >= QuestBalance.Active.MaxQuestDays)
        {
            Log.Info($"    QUEST ABANDONED after {quest.DaysSpent} days");
            KillParty(quest, now);
            FinishQuest(quest, now);
            return;
        }

        var at = quest.LastProcessedAt;

        while (true)
        {
            // Фаза идёт — ждём окончания
            if (quest.CurrentPhaseEndTime.HasValue)
            {
                if (quest.CurrentPhaseEndTime.Value > now) break;
                at = quest.CurrentPhaseEndTime.Value;
                ResolveCurrentPhase(quest, at);
                quest.CurrentPhaseEndTime = null;
                if (quest.Status == QuestStatus.Completed) return;
                continue;
            }

            // Ночь — ждём окончания
            if (quest.IsResting)
            {
                if (quest.RestEndTime.Value > now) break;
                at = quest.RestEndTime.Value;
                ProcessNightResult(quest, at);
                quest.IsResting = false;
                quest.RestEndTime = null;
                quest.DaysSpent++;
                quest.MinutesUsedToday = 0;
                continue;
            }

            // Получить следующую фазу (ambient или основную)
            var next = SelectNextPhase(quest);

            // Все фазы пройдены
            if (next == null)
            {
                FinishQuest(quest, at);
                quest.LastProcessedAt = at;
                return;
            }

            // Не влезает в день — начинаем ночь
            if (quest.MinutesUsedToday + next.DurationMinutes > GameTime.MinutesPerDaylight)
            {
                Log.Info($"    DAY {quest.DaysSpent + 1} ends | rest {GameTime.MinutesPerNight}m");
                quest.IsResting = true;
                quest.RestEndTime = at + GameTime.FromMinutes(GameTime.MinutesPerNight);
                continue;
            }

            // Начинаем фазу
            quest.CurrentPhaseEndTime = at + GameTime.FromMinutes(next.DurationMinutes);
            quest.LastProcessedAt = at;
            return;
        }

        if (at > quest.LastProcessedAt)
            quest.LastProcessedAt = at;
    }

    // === Выбор следующей фазы ===

    private QuestPhaseInstance SelectNextPhase(QuestModel quest)
    {
        // 1. Ambient вставляются блоком после первой основной фазы
        if (quest.AmbientIndex < quest.AmbientPhases.Count && ShouldInsertAmbient(quest))
            return quest.AmbientPhases[quest.AmbientIndex];

        // 2. Основная фаза по CurrentRoleId
        if (string.IsNullOrEmpty(quest.CurrentRoleId)) return null;

        foreach (var phase in quest.Phases)
            if (phase.RoleId == quest.CurrentRoleId) return phase;

        return null;
    }

    private static bool ShouldInsertAmbient(QuestModel quest)
    {
        if (quest.AmbientIndex >= quest.AmbientPhases.Count) return false;
        if (quest.PhaseResults.Count == 0) return false;

        var last = quest.PhaseResults[^1];

        // Внутри ambient-блока — продолжаем
        if (last.Phase.RoleId == null) return true;

        // Только что закончили первую основную — начинаем блок
        return quest.AmbientIndex == 0;
    }

    private QuestPhaseInstance FindCurrentPhase(QuestModel quest)
    {
        if (quest.AmbientIndex < quest.AmbientPhases.Count && ShouldInsertAmbient(quest))
            return quest.AmbientPhases[quest.AmbientIndex];

        foreach (var phase in quest.Phases)
            if (phase.RoleId == quest.CurrentRoleId) return phase;

        return null;
    }

    // === Резолв фазы ===

    private void ResolveCurrentPhase(QuestModel quest, GameTime at)
    {
        var phase = FindCurrentPhase(quest);
        if (phase == null) return;

        var outcome = QuestCalculator.EvaluatePhase(
            phase, quest.Party, quest.CodeLevel, woundRng);

        bool passed = outcome.Passed;

        quest.PhaseResults.Add(new PhaseResult
        {
            Phase = phase,
            Passed = passed,
            EffectiveCritical = phase.Critical,
            ExpEarned = passed ? phase.ExpReward : 0,
            DayCompleted = quest.DaysSpent,
            Attempts = 1,
            Rolls = outcome.Rolls,
        });

        quest.MinutesUsedToday += phase.DurationMinutes;

        string tag = phase.RoleId ?? "ambient";
        string targetTag = !string.IsNullOrEmpty(quest.TargetId)
            ? $" vs {quest.TargetId}"
            : "";

        if (passed)
        {
            Log.Info($"    DAY {quest.DaysSpent + 1} | '{tag}' OK   " +
                     $"+{phase.ExpReward}xp | used={quest.MinutesUsedToday}/{GameTime.MinutesPerDaylight}m");
            LogRolls(outcome.Rolls);
            AdvanceAfterPhase(quest, phase, passed: true);
        }
        else if (phase.Critical)
        {
            Log.Info($"    DAY {quest.DaysSpent + 1} | '{tag}' CRIT FAIL{targetTag} | quest ends");
            LogRolls(outcome.Rolls);
            ApplyCombatOutcome(quest, at);
            quest.CurrentRoleId = null;
        }
        else
        {
            Log.Info($"    DAY {quest.DaysSpent + 1} | '{tag}' FAIL | " +
                     $"used={quest.MinutesUsedToday}/{GameTime.MinutesPerDaylight}m");
            LogRolls(outcome.Rolls);
            AdvanceAfterPhase(quest, phase, passed: false);

            if (phase.DurationMinutes >= 240)
            {
                quest.MinutesUsedToday += 60;
                Log.Info($"      +short_rest 60m");
            }
        }
    }

    private static void AdvanceAfterPhase(QuestModel quest, QuestPhaseInstance phase, bool passed)
    {
        // Ambient — просто сдвигаем счётчик
        if (phase.RoleId == null)
        {
            quest.AmbientIndex++;
            return;
        }

        // Основная — переход по графу
        var transition = passed ? phase.NextOnPass : phase.NextOnFail;

        if (transition.HasValue)
        {
            quest.CurrentRoleId = transition.Value.ToRoleId;
            ApplyTransitionDelta(quest, transition.Value);
        }
        else
        {
            quest.CurrentRoleId = null;
        }
    }

    private static void ApplyTransitionDelta(QuestModel quest, QuestTransition transition)
    {
        if (Math.Abs(transition.DcDelta) < 1e-9 && transition.DurationDelta == 0) return;

        foreach (var phase in quest.Phases)
        {
            if (phase.RoleId != transition.ToRoleId) continue;

            if (Math.Abs(transition.DcDelta) > 1e-9)
            {
                foreach (var sol in phase.Solutions)
                {
                    var keys = new List<string>(sol.Skills.Keys);
                    foreach (var k in keys)
                        sol.Skills[k] += transition.DcDelta;
                }
            }

            phase.DurationMinutes += transition.DurationDelta;
            break;
        }
    }

    private static void LogRolls(List<SkillRollResult> rolls)
    {
        if (rolls == null || rolls.Count == 0) return;

        foreach (var r in rolls)
        {
            string mark;
            if (!r.Passed) mark = "✗";
            else if (r.Ratio >= 1.0) mark = "✓";
            else mark = "!";

            Log.Info($"      {mark} [s{r.SolutionIndex + 1}] {r.SkillId,-12} " +
                     $"{r.SkillValue,6:F1}/{r.Required,6:F1} " +
                     $"ratio={r.Ratio:F2} chance={r.Chance:F0}% roll={r.Roll:F1} " +
                     $"({r.BestAdvName})");
        }
    }

    // === Ночь ===

    private void ProcessNightResult(QuestModel quest, GameTime at)
    {
        double attackChance = QuestCalculator.NightAttackChance(quest);

        if (!RollChance(attackChance))
        {
            Log.Info($"    NIGHT {quest.DaysSpent + 1} → peaceful");
            return;
        }

        Log.Info($"    NIGHT {quest.DaysSpent + 1} → attacked");

        int nightLevel = Math.Max(1, quest.CodeLevel - QuestBalance.Active.NightCreatureLevelPenalty);

        var combatPhase = BuildNightCombatPhase(quest, nightLevel);
        if (combatPhase == null)
        {
            Log.Info($"    NIGHT {quest.DaysSpent + 1} → peaceful (no combat phase)");
            return;
        }

        var combatOutcome = QuestCalculator.EvaluatePhase(
            combatPhase, quest.Party, nightLevel, woundRng);

        if (combatOutcome.Passed)
        {
            Log.Info($"    NIGHT {quest.DaysSpent + 1} → survived");
            var creature = !string.IsNullOrEmpty(quest.TargetId)
                ? QuestDatabase.GetCreature(quest.TargetId)
                : null;
            ApplyLightWounds(quest, creature, at);
        }
        else
        {
            Log.Info($"    NIGHT {quest.DaysSpent + 1} → defeated");
            ApplyCombatOutcome(quest, at);
            quest.CurrentRoleId = null;
            quest.AmbientIndex = quest.AmbientPhases.Count;
        }
    }

    private static QuestPhaseInstance BuildNightCombatPhase(QuestModel quest, int nightLevel)
    {
        if (!QuestDatabase.Phases.TryGetValue("combat", out var template))
            return null;

        var target = !string.IsNullOrEmpty(quest.TargetId)
            ? QuestDatabase.GetCreature(quest.TargetId)
            : null;
        if (target == null) return null;

        var balance = AdventurerBalance.Active;
        var stats = new CreatureStats(target, nightLevel, balance);
        double refMax = balance.StatStartPool + (nightLevel - 1) * balance.StatBaseSlope;

        var solutions = new List<PhaseSolution>();

        if (template.Paths != null)
        {
            foreach (var path in template.Paths)
            {
                var sol = new PhaseSolution { Skills = new Dictionary<string, double>() };

                if (path.Checks != null)
                {
                    foreach (var check in path.Checks)
                    {
                        double dc = ComputeCheckDc(check, stats, refMax);
                        if (dc >= 0) sol.Skills[check.Skill] = dc;
                    }
                }

                if (sol.Skills.Count > 0) solutions.Add(sol);
            }
        }

        if (solutions.Count == 0) return null;

        return new QuestPhaseInstance
        {
            RoleId = null,
            PhaseId = template.Id,
            Name = template.Name,
            Solutions = solutions,
            DurationMinutes = 5,
            ExpReward = 0,
            Critical = false,
            NextOnPass = default,
            NextOnFail = default,
        };
    }

    private static double ComputeCheckDc(PhaseCheck check, CreatureStats stats, double refMax)
    {
        if (refMax <= 0) return -1;

        if (check.Resistance == null || check.Resistance.Count == 0)
        {
            string statId = AdventurerDatabase.StatForSkill(check.Skill);
            if (statId == null) return -1;
            return stats.Get(statId) / refMax;
        }

        double sum = 0;
        int count = 0;
        foreach (var statId in check.Resistance)
        {
            sum += stats.Get(statId);
            count++;
        }

        if (count == 0) return -1;
        return (sum / count) / refMax;
    }

    // === Травмы и смерть ===

    private void ApplyCombatOutcome(QuestModel quest, GameTime at)
    {
        var creature = !string.IsNullOrEmpty(quest.TargetId)
            ? QuestDatabase.GetCreature(quest.TargetId)
            : null;

        var b = QuestBalance.Active;

        foreach (var a in quest.Party)
        {
            if (a.HasStatus(StatusIds.Dead))
            {
                Log.Info($"    {a.FullName,-24} skip (already dead)");
                continue;
            }

            double end = EnduranceOf(a);
            double injuryShield = InjuryShield(end);
            double escapeChance = QuestCalculator.ComputeEscapeChance(a, creature, quest.CodeLevel);

            if (RollChance(escapeChance))
            {
                var woundDays = b.WoundFailDays;
                int days = woundRng.Next(woundDays.Min, woundDays.Max + 1);
                a.SetStatus(StatusIds.Wounded, at + GameTime.FromDays(days));

                Log.Info($"    {a.FullName,-24} esc {escapeChance,4:F0}% OK  " +
                         $"end={end,6:F1} inj={injuryShield,4:P0} → wounded {days}d");
            }
            else
            {
                var deadStatus = AdventurerDatabase.GetStatus(StatusIds.Dead);
                var days = deadStatus?.AutoRemoveAfterDays ?? 4;
                a.SetStatus(StatusIds.Dead, at + GameTime.FromDays(days));

                Log.Info($"    {a.FullName,-24} esc {escapeChance,4:F0}% FAIL " +
                         $"end={end,6:F1} inj={injuryShield,4:P0} → DIED");
            }
        }
    }

    private void ApplyLightWounds(QuestModel quest, CreatureInfo creature, GameTime at)
    {
        var b = QuestBalance.Active;
        var success = b.WoundSuccess;

        foreach (var a in quest.Party)
        {
            if (a.HasStatus(StatusIds.Dead)) continue;

            double end = EnduranceOf(a);
            double injuryShield = InjuryShield(end);
            double chance = success.Chance * (1.0 - injuryShield);

            if (RollChance(chance))
            {
                int days = woundRng.Next(success.Min, success.Max + 1);
                a.SetStatus(StatusIds.Wounded, at + GameTime.FromDays(days));
                Log.Info($"    {a.FullName,-24} light wound {days}d");
            }
        }
    }

    private void KillParty(QuestModel quest, GameTime at)
    {
        foreach (var a in quest.Party)
        {
            if (a.HasStatus(StatusIds.Dead)) continue;
            var deadStatus = AdventurerDatabase.GetStatus(StatusIds.Dead);
            var days = deadStatus?.AutoRemoveAfterDays ?? 4;
            a.SetStatus(StatusIds.Dead, at + GameTime.FromDays(days));
        }
    }

    // === Финал ===

    private void FinishQuest(QuestModel quest, GameTime at)
    {
        quest.Result = resolver.Resolve(quest);
        quest.Status = QuestStatus.Completed;

        registry.CompleteToHistory(quest);

        int totalXpGiven = DistributeExperience(quest);
        LogCompletion(quest, totalXpGiven);

        foreach (var a in quest.Party)
        {
            if (a.HasStatus(StatusIds.Dead)) continue;
            if (a.HasStatus(StatusIds.Wounded)) continue;
            a.SetStatus(StatusIds.Free);
        }

        bank.AddGold(quest.Result.GoldEarned);
        bank.AddGlory(quest.Result.GloryEarned);

        Completed?.Invoke(quest);
    }

    // === Опыт ===

    private int DistributeExperience(QuestModel quest)
    {
        var phaseResults = quest.PhaseResults ?? new List<PhaseResult>();
        int totalExp = QuestCalculator.TotalExp(phaseResults);
        if (totalExp <= 0) return 0;

        var aliveParty = quest.Party
            .Where(a => !a.HasStatus(StatusIds.Dead))
            .ToList();

        if (aliveParty.Count == 0)
        {
            Log.Info("    experience: no survivors");
            return 0;
        }

        var shares = QuestCalculator.ContributionShares(aliveParty, phaseResults, quest.CodeLevel);

        var exact = new List<(AdventurerModel a, int xp, double frac)>();
        int distributed = 0;

        foreach (var a in aliveParty)
        {
            if (!shares.TryGetValue(a, out var share)) continue;

            double e = totalExp * share;
            int fl = (int)Math.Floor(e);
            if (fl < 0) fl = 0;

            distributed += fl;
            exact.Add((a, fl, e - fl));
        }

        int remainder = totalExp - distributed;
        if (remainder < 0) remainder = 0;

        exact.Sort((x, y) => y.frac.CompareTo(x.frac));

        var xpByAdv = new Dictionary<AdventurerModel, int>();
        foreach (var (a, fl, _) in exact) xpByAdv[a] = fl;
        for (int i = 0; i < exact.Count && remainder > 0; i++)
        {
            xpByAdv[exact[i].a] += 1;
            remainder--;
        }

        int totalXpGiven = 0;

        foreach (var a in aliveParty)
        {
            if (!xpByAdv.TryGetValue(a, out var xp) || xp <= 0) continue;

            int levelBefore = a.Level.Number;

            AdventurerProgress.AddExperience(a, xp);

            int levelAfter = a.Level.Number;
            bool leveled = levelAfter > levelBefore;

            totalXpGiven += xp;

            Log.Info($"    {a.FullName,-24} {a.ClassId,-10} " +
                     $"+{xp,3} XP  share={shares[a]:P0}  " +
                     (leveled ? $"→ L{levelAfter}" : $"{a.Level.Experience,4}/{a.Level.ExpToNext,4}"));
        }

        XpStats.Record(quest.CodeLevel, totalXpGiven);
        return totalXpGiven;
    }

    // === Утилиты ===

    private static double EnduranceOf(AdventurerModel a)
    {
        if (a?.Stats == null) return 0;
        return a.Stats.TryGetValue(StatIds.Endurance, out var end) ? end : 0;
    }

    private static double InjuryShield(double end)
    {
        if (end <= 0) return 0;
        var b = QuestBalance.Active;
        double s = end / (end + b.EnduranceInjuryK);
        return Math.Min(s, b.EnduranceInjuryCap);
    }

    private bool RollChance(double percent)
        => percent > 0 && woundRng.NextDouble() * 100.0 < percent;

    // === Логи ===

    private static void LogStart(QuestModel quest, IReadOnlyList<AdventurerModel> party)
    {
        string name = quest.Name ?? "";
        if (name.Length > 24) name = name[..24];

        string target = quest.TargetId ?? "?";
        string location = quest.LocationId ?? "?";

        string diffStr = $"d={quest.DifficultyModifier:+0.00;-0.00;+0.00}";
        Log.Info($"[Q] >>> PLAN   L{quest.CodeLevel,-2} {diffStr} '{name}' " +
                 $"depart={quest.StartTime} party={party.Count} " +
                 $"target={target} location={location}");

        var members = string.Join(", ",
            party.Select(a => $"{a.FirstName} ({a.RaceId} {a.ClassId} L{a.Level.Number})"));
        Log.Info($"    party: {members}");

        foreach (var phase in quest.Phases)
        {
            string crit = phase.Critical ? "[C]" : "   ";
            Log.Info($"    {crit} {phase.RoleId,-14} ({phase.PhaseId,-12}) " +
                     $"dur={phase.DurationMinutes,4}m exp={phase.ExpReward,3}");
        }

        if (quest.AmbientPhases != null && quest.AmbientPhases.Count > 0)
        {
            Log.Info($"    ambient: {quest.AmbientPhases.Count}");
            foreach (var phase in quest.AmbientPhases)
                Log.Info($"       {phase.PhaseId,-12} dur={phase.DurationMinutes,4}m exp={phase.ExpReward,3}");
        }
    }

    private static void LogDepart(QuestModel quest)
    {
        Log.Info($"[Q] >>> DEPART {quest.StartTime} | {quest.Party.Count} adventurers");
    }

    private static void LogCompletion(QuestModel quest, int totalXpGiven)
    {
        string role = quest.Result.Role switch
        {
            QuestGradeRole.Triumph => "TRI",
            QuestGradeRole.Success => "SUC",
            QuestGradeRole.Failure => "FAI",
            QuestGradeRole.Disaster => "DIS",
            _ => "???"
        };

        string name = quest.Name ?? "";
        if (name.Length > 24) name = name[..24];

        var pr = quest.PhaseResults ?? new List<PhaseResult>();
        int passed = pr.Count(r => r.Passed);
        int total = pr.Count;
        int critPassed = pr.Count(r => r.EffectiveCritical && r.Passed);
        int critTotal = pr.Count(r => r.EffectiveCritical);
        int totalExp = QuestCalculator.TotalExp(pr);

        string failTag = quest.Result.FailedByCritical ? " FAILED" : "";
        string target = quest.TargetId ?? "?";
        string location = quest.LocationId ?? "?";

        string diffStr = $"d={quest.DifficultyModifier:+0.00;-0.00;+0.00}";
        Log.Info($"[Q] >>> FINAL  L{quest.CodeLevel,-2} {diffStr} {role}{failTag} '{name}'  " +
                 $"days={quest.DaysSpent + 1} target={target} location={location}");

        Log.Info($"    phases={passed}/{total} crit={critPassed}/{critTotal} " +
                 $"exp={totalXpGiven}/{totalExp} " +
                 $"gold={quest.Result.GoldEarned} glory={quest.Result.GloryEarned}");
    }

    // === Доска ===

    private void RefreshBoard()
    {
        while (registry.CountAvailable() < BoardCapacity)
        {
            int codeLevel = RollQuestLevel();
            var quest = factory.Create(clock.Now, codeLevel);
            if (quest == null) break;

            quest.Status = QuestStatus.Available;
            registry.Add(quest);
            Added?.Invoke(quest);
        }
    }

    private int RollQuestLevel()
    {
        // TODO: правильное распределение уровней квестов. Пока — уровень 1.
        int baseLevel = 1;
        int roll = woundRng.Next(0, 1);
        return Math.Clamp(baseLevel + roll, 1, AdventurerBalance.MaxLevel);
    }
}