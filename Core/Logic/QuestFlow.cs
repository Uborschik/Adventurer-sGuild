using System;
using System.Collections.Generic;
using System.Linq;

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

    public QuestFlow(QuestRegistry registry, QuestFactory factory, QuestResolver resolver,
                     GameClock clock, GuildBank bank, int? seed = null)
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

        // Все должны быть живы, свободны и без дубликатов
        var seen = new HashSet<string>();
        foreach (var a in party)
        {
            if (a == null) return false;
            if (!seen.Add(a.Id)) return false;
            if (a.HasStatus(StatusIds.Dead)) return false;
        }

        quest.Status = QuestStatus.InProgress;
        quest.Party = new List<AdventurerModel>(party);
        quest.StartTime = clock.Now;
        quest.EndTime = clock.Now + GameTime.FromMinutes(quest.DurationMinutes);

        foreach (var a in quest.Party)
            a.SetStatus(StatusIds.OnQuest);

        Started?.Invoke(quest);
        return true;
    }

    // === Обработка времени ===

    private void OnTimeAdvanced(GameTime now)
    {
        ExpireOld(now);
        CompleteReady(now);
        RefreshBoard();
    }

    private void ExpireOld(GameTime now)
    {
        foreach (var quest in registry.Available.ToList())
        {
            if (now < quest.ExpiresAt) continue;

            quest.Status = QuestStatus.Expired;
            Expired?.Invoke(quest);
        }
    }

    private void CompleteReady(GameTime now)
    {
        foreach (var quest in registry.InProgress.ToList())
        {
            if (now < quest.EndTime) continue;

            quest.Result = resolver.Resolve(quest);
            quest.Status = QuestStatus.Completed;

            var outcome = QuestDatabase.GetOutcome(quest.Result.Role);

            // === 1. Смерть и травмы (один раз на авантюриста) ===
            foreach (var a in quest.Party)
            {
                if (a.HasStatus(StatusIds.Dead))
                {
                    Log.Info($"    {a.FullName,-24} skip (already dead)");
                    continue;
                }

                int end = EnduranceOf(a);
                double injuryShield = InjuryShield(end);
                double deathShield = DeathShield(end);

                string chances = BuildChanceInfo(outcome, injuryShield, deathShield);

                double rawDeath = outcome?.DeathChance ?? 0;
                double effDeath = rawDeath * (1.0 - deathShield);

                if (RollChance(effDeath))
                {
                    var deadStatus = AdventurerDatabase.GetStatus(StatusIds.Dead);
                    var days = deadStatus?.AutoRemoveAfterDays ?? 4;
                    a.SetStatus(StatusIds.Dead, now + GameTime.FromDays(days));

                    Log.Info($"    {a.FullName,-24} end={end,3} inj={injuryShield,4:P0} dth={deathShield,4:P0} | {chances} | DIED");
                    continue;
                }

                var picked = PickStatus(outcome, now, injuryShield);

                if (picked == null)
                {
                    a.SetStatus(StatusIds.Free);
                    Log.Info($"    {a.FullName,-24} end={end,3} inj={injuryShield,4:P0} dth={deathShield,4:P0} | {chances} | ok");
                }
                else
                {
                    a.SetStatus(picked.Value.Id, picked.Value.ExpiresAt);
                    Log.Info($"    {a.FullName,-24} end={end,3} inj={injuryShield,4:P0} dth={deathShield,4:P0} | {chances} | {picked.Value.Id}");
                }
            }

            // === 2. Опыт — только живым ===
            double pool = QuestCalculator.TotalExpPool(
                quest.Level, quest.Tier, quest.Result.Role);

            int totalXpGiven = 0;

            var aliveParty = quest.Party
                .Where(a => !a.HasStatus(StatusIds.Dead))
                .ToList();

            if (pool > 0 && aliveParty.Count > 0)
            {
                var shares = QuestCalculator.ContributionShares(aliveParty, quest.Weights);

                // floor + remainder: распределяем весь пул без потерь
                var exact = new List<(AdventurerModel a, int xp, double frac)>();
                int distributed = 0;

                foreach (var a in aliveParty)
                {
                    if (!shares.TryGetValue(a, out var share)) continue;

                    double e = pool * share;
                    int fl = (int)Math.Floor(e);
                    if (fl < 0) fl = 0;

                    distributed += fl;
                    exact.Add((a, fl, e - fl));
                }

                int remainder = (int)Math.Round(pool) - distributed;
                if (remainder < 0) remainder = 0;

                exact.Sort((x, y) => y.frac.CompareTo(x.frac));

                var xpByAdv = new Dictionary<AdventurerModel, int>();
                foreach (var (a, fl, _) in exact) xpByAdv[a] = fl;
                for (int i = 0; i < exact.Count && remainder > 0; i++)
                {
                    xpByAdv[exact[i].a] += 1;
                    remainder--;
                }

                foreach (var a in aliveParty)
                {
                    if (!xpByAdv.TryGetValue(a, out var xp) || xp <= 0) continue;

                    int levelBefore = a.Level.Number;
                    int expBefore = a.Level.Experience;

                    AdventurerProgress.AddExperience(a, xp);

                    int levelAfter = a.Level.Number;
                    bool leveled = levelAfter > levelBefore;

                    totalXpGiven += xp;

                    Log.Info(
                        $"    {a.FullName,-24} {a.ClassId,-10} L{levelBefore,-2} " +
                        $"exp {expBefore,4}/{a.Level.ExpToNext,4}  +{xp,4} XP" +
                        (leveled ? $"  -> LEVEL UP L{levelAfter}" : ""));
                }

                XpStats.Record(quest.Level, totalXpGiven);
            }

            LogCompletion(quest, pool, totalXpGiven);

            bank.AddGold(quest.Result.GoldEarned);
            bank.AddGlory(quest.Result.GloryEarned);

            Completed?.Invoke(quest);
        }
    }

    // === Endurance-щит ===

    private static int EnduranceOf(AdventurerModel a)
    {
        if (a?.Stats == null) return 0;
        return a.Stats.TryGetValue(StatIds.Endurance, out var end) ? end : 0;
    }

    private static double InjuryShield(int end)
    {
        if (end <= 0) return 0;
        var b = QuestBalance.Active;
        double s = end / (end + b.EnduranceInjuryK);
        return Math.Min(s, b.EnduranceInjuryCap);
    }

    private static double DeathShield(int end)
    {
        if (end <= 0) return 0;
        var b = QuestBalance.Active;
        double s = end / (end + b.EnduranceDeathK);
        return Math.Min(s, b.EnduranceDeathCap);
    }

    // === Статусы после квеста ===

    private struct PickedStatus
    {
        public string Id;
        public GameTime? ExpiresAt;
    }

    private PickedStatus? PickStatus(QuestOutcomeInfo outcome, GameTime now, double injuryShield)
    {
        if (outcome?.Statuses == null || outcome.Statuses.Count == 0)
            return null;

        foreach (var s in outcome.Statuses)
        {
            double eff = s.Chance * (1.0 - injuryShield);
            if (!RollChance(eff)) continue;

            int days = s.DurationDaysMax > s.DurationDaysMin
                ? woundRng.Next(s.DurationDaysMin, s.DurationDaysMax + 1)
                : s.DurationDaysMin;

            return new PickedStatus
            {
                Id = s.Id,
                ExpiresAt = now + GameTime.FromDays(days),
            };
        }

        return null;
    }

    private bool RollChance(double percent) => percent > 0 && woundRng.NextDouble() * 100.0 < percent;

    // === Логи ===

    private static string BuildChanceInfo(QuestOutcomeInfo outcome, double injuryShield, double deathShield)
    {
        if (outcome == null) return "no outcome";

        var parts = new List<string>();

        double rawDeath = outcome.DeathChance;
        double effDeath = rawDeath * (1.0 - deathShield);
        parts.Add($"death {rawDeath,4:F1}->{effDeath:F1}");

        if (outcome.Statuses != null && outcome.Statuses.Count > 0)
        {
            foreach (var s in outcome.Statuses)
            {
                double eff = s.Chance * (1.0 - injuryShield);
                parts.Add($"{s.Id} {s.Chance,4:F1}->{eff:F1}");
            }
        }
        else
        {
            parts.Add("no injuries");
        }

        return string.Join(" | ", parts);
    }

    private static void LogCompletion(QuestModel quest, double pool, int totalXpGiven)
    {
        string tier =
            quest.Tier == QuestTier.Easy ? "Esy" :
            quest.Tier == QuestTier.Hard ? "Hrd" : "Nrm";

        string role = quest.Result.Role switch
        {
            QuestGradeRole.Triumph => "TRI",
            QuestGradeRole.Success => "SUC",
            QuestGradeRole.Failure => "FAI",
            QuestGradeRole.Disaster => "DIS",
            _ => "???"
        };

        string name = quest.Name ?? "";
        if (name.Length > 20) name = name[..20];

        Log.Info(
            $"[Q] L{quest.Level,-2} {tier} {role} '{name}' " +
            $"party={quest.Party.Count} pool={pool:F0} given={totalXpGiven} " +
            $"weights={FormatWeights(quest.Weights)}");
    }

    private static string FormatWeights(IReadOnlyDictionary<string, double> weights)
    {
        if (weights == null || weights.Count == 0) return "-";

        var parts = new List<string>();
        foreach (var kv in weights)
            if (kv.Value > 0)
                parts.Add($"{kv.Key}={kv.Value:F2}");
        return string.Join(" ", parts);
    }

    // === Доска ===

    private void RefreshBoard()
    {
        var available = registry.CountAvailable();

        while (available < BoardCapacity)
        {
            var quest = factory.Create(clock.Now);

            if (quest == null) break;

            registry.Add(quest);
            Added?.Invoke(quest);
            available++;
        }
    }
}