using System;
using System.Collections.Generic;
using System.Linq;

public enum QuestTier { Easy, Normal, Hard }

public struct PhaseResult
{
    public QuestPhaseTemplate Phase;
    public bool Passed;
    public bool EffectiveCritical;
    public int ExpEarned;
    public int DayCompleted;
    public int Attempts;
}

public static class QuestCalculator
{
    private static QuestBalanceProfile B => QuestBalance.Active;

    public const double PrimarySkillBonus = 1.20;
    public const double SecondarySkillBonus = 1.10;

    // === Базовая нормализация ===

    public static double ReferenceMaxStatQuest(int codeLevel)
        => 16 + (codeLevel - 1) * 2.5;

    public static double EffectiveStat(AdventurerModel a, string statId)
    {
        if (a?.Stats == null) return 0;
        if (!a.Stats.TryGetValue(statId, out var v)) return 0;
        double woundedFactor = a.HasStatus(StatusIds.Wounded) ? 0.5 : 1.0;
        return woundedFactor * v;
    }

    public static double SkillValue(AdventurerModel a, string skillId)
    {
        if (a?.Stats == null) return 0;

        string statId = AdventurerDatabase.StatForSkill(skillId);
        if (statId == null) return 0;

        double statValue = EffectiveStat(a, statId);

        var cls = AdventurerDatabase.GetClass(a.ClassId);
        if (cls != null)
        {
            if (cls.PrimarySkill == skillId)
                return statValue * PrimarySkillBonus;

            if (cls.SecondarySkill == skillId)
                return statValue * SecondarySkillBonus;
        }

        return statValue;
    }

    // === Проверка фазы (командная) ===

    public static bool PhasePassed(QuestPhaseTemplate phase,
                                    IReadOnlyList<AdventurerModel> party,
                                    int codeLevel,
                                    double extraDc = 0)
    {
        if (phase?.Solutions == null || party == null || party.Count == 0)
            return false;

        double refMax = ReferenceMaxStatQuest(codeLevel);

        foreach (var sol in phase.Solutions)
        {
            if (sol.Skills == null || sol.Skills.Count == 0) continue;

            bool allCovered = true;

            foreach (var kv in sol.Skills)
            {
                double required = (kv.Value + extraDc) * refMax;

                bool covered = false;
                foreach (var a in party)
                {
                    if (a.HasStatus(StatusIds.Dead)) continue;

                    if (SkillValue(a, kv.Key) >= required)
                    {
                        covered = true;
                        break;
                    }
                }

                if (!covered)
                {
                    allCovered = false;
                    break;
                }
            }

            if (allCovered) return true;
        }

        return false;
    }

    public static double NightAttackChance(QuestModel quest)
    {
        var b = QuestBalance.Active;
        return Math.Clamp(b.NightAttackChanceBase, 0, 100);
    }

    // === Роль ===

    public static QuestGradeRole DetermineRole(List<PhaseResult> results)
    {
        if (results == null || results.Count == 0) return QuestGradeRole.Disaster;

        int criticalTotal = 0, criticalPassed = 0;
        int normalTotal = 0, normalPassed = 0;

        foreach (var r in results)
        {
            if (r.EffectiveCritical)
            {
                criticalTotal++;
                if (r.Passed) criticalPassed++;
            }
            else
            {
                normalTotal++;
                if (r.Passed) normalPassed++;
            }
        }

        if (criticalPassed == criticalTotal && normalPassed == normalTotal)
            return QuestGradeRole.Triumph;

        if (criticalPassed == criticalTotal)
            return QuestGradeRole.Success;

        if (criticalPassed == 0 && normalPassed == 0)
            return QuestGradeRole.Disaster;

        return QuestGradeRole.Failure;
    }

    public static bool IsQuestFailed(List<PhaseResult> results)
    {
        if (results == null) return true;
        foreach (var r in results)
            if (r.EffectiveCritical && !r.Passed) return true;
        return false;
    }

    public static int TotalExp(List<PhaseResult> results)
    {
        if (results == null) return 0;
        int sum = 0;
        foreach (var r in results) sum += r.ExpEarned;
        return sum;
    }

    // === Contribution ===

    public static double Contribution(AdventurerModel a, List<PhaseResult> results)
    {
        if (a == null || results == null || results.Count == 0) return 0;

        double baseScore = B.Experience?.ContributionBaseScore ?? 0.2;
        double score = 0;
        int passedCount = 0;

        foreach (var r in results)
        {
            if (!r.Passed) continue;
            passedCount++;

            double bestForPhase = 0;
            foreach (var sol in r.Phase.Solutions)
            {
                if (sol.Skills == null || sol.Skills.Count == 0) continue;

                double worstInSolution = double.MaxValue;
                foreach (var kv in sol.Skills)
                {
                    if (kv.Value <= 0) { worstInSolution = 0; break; }
                    double norm = SkillValue(a, kv.Key);
                    double relative = norm / kv.Value;
                    if (relative < worstInSolution) worstInSolution = relative;
                }
                if (worstInSolution > bestForPhase) bestForPhase = worstInSolution;
            }
            score += Math.Min(bestForPhase, 1.0);
        }

        double phaseScore = passedCount > 0 ? score / passedCount : 0;
        return baseScore + phaseScore;
    }

    public static Dictionary<AdventurerModel, double> ContributionShares(
        IReadOnlyList<AdventurerModel> party,
        List<PhaseResult> results)
    {
        var result = new Dictionary<AdventurerModel, double>();
        if (party == null || party.Count == 0) return result;

        double total = 0;
        var scores = new double[party.Count];

        for (int i = 0; i < party.Count; i++)
        {
            scores[i] = Contribution(party[i], results);
            total += scores[i];
        }

        if (total <= 0)
        {
            double even = 1.0 / party.Count;
            foreach (var a in party) result[a] = even;
            return result;
        }

        for (int i = 0; i < party.Count; i++)
            result[party[i]] = scores[i] / total;

        return result;
    }

    // === Escape при провале combat ===

    public static double ComputeEscapeChance(AdventurerModel a, CreatureInfo creature, int codeLevel)
    {
        var b = B;

        double baseEscape = b.EscapeBaseByTier.Get(creature?.Tier);
        double combatDcDelta = ExtractCombatDcDelta(creature);
        double creatureMod = -combatDcDelta * 100;

        double refMax = ReferenceMaxStatQuest(codeLevel);

        double str = EffectiveStat(a, StatIds.Strength) / refMax;
        double dex = EffectiveStat(a, StatIds.Dexterity) / refMax;
        double end = EffectiveStat(a, StatIds.Endurance) / refMax;
        double wis = EffectiveStat(a, StatIds.Wisdom) / refMax;

        double best = Math.Max(Math.Max(str, dex), Math.Max(end * 0.8, wis * 0.5));
        double statBonus = Math.Min(b.EscapeStatCap, best * b.EscapeStatK);

        double final = baseEscape + creatureMod + statBonus;
        return Math.Clamp(final, 0, b.EscapeStatCap);
    }

    private static double ExtractCombatDcDelta(CreatureInfo creature)
    {
        if (creature?.PhaseEffects?.Modifies == null) return 0;
        if (!creature.PhaseEffects.Modifies.TryGetValue("combat", out var mod)) return 0;

        if (mod.DcDelta.HasValue) return mod.DcDelta.Value;

        if (mod.StatModifiers != null && mod.StatModifiers.Count > 0)
        {
            double sum = 0;
            foreach (var kv in mod.StatModifiers) sum += kv.Value.DcDelta;
            return sum / mod.StatModifiers.Count;
        }

        return 0;
    }
}