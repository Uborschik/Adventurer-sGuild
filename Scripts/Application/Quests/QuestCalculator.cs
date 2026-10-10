using System;
using System.Collections.Generic;
using AdventurersGuild.Data.Adventurer;
using AdventurersGuild.Data.Balance;
using AdventurersGuild.Data.Quest;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Quests;

namespace AdventurersGuild.Application.Quests;

public struct SkillRollResult
{
    public string SkillId;
    public string BestAdvName;
    public double SkillValue;
    public double Required;
    public double Ratio;
    public double Chance;
    public double Roll;
    public bool Passed;
    public int SolutionIndex;
}

public struct PhaseOutcome
{
    public bool Passed;
    public List<SkillRollResult> Rolls;
}

public static class QuestCalculator
{
    private static QuestBalanceProfile B => QuestBalance.Active;

    // === Базовая нормализация ===

    public static double ReferenceMaxStatQuest(int codeLevel)
    {
        var ab = AdventurerBalance.Active;
        return ab.StatStartPool + (codeLevel - 1) * ab.StatBaseSlope;
    }

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

        var skill = AdventurerDatabase.GetSkill(skillId);
        if (skill == null) return 0;

        string statId = AdventurerDatabase.StatForSkill(skillId);
        if (statId == null) return 0;

        double statValue = EffectiveStat(a, statId);

        double classMod = 1.0;
        if (skill.ClassModifiers != null
            && skill.ClassModifiers.TryGetValue(a.ClassId, out var cm))
            classMod = cm;

        return statValue * skill.StatModifier * classMod;
    }

    // === Проверка фазы (командная) ===

    public static PhaseOutcome EvaluatePhase(
        QuestPhaseInstance phase,
        IReadOnlyList<AdventurerModel> party,
        int codeLevel,
        Random rng,
        double extraDc = 0)
    {
        var outcome = new PhaseOutcome
        {
            Passed = false,
            Rolls = new List<SkillRollResult>(),
        };

        if (phase?.Solutions == null || party == null || party.Count == 0)
            return outcome;

        if (rng == null)
            throw new ArgumentNullException(nameof(rng));

        double refMax = ReferenceMaxStatQuest(codeLevel);
        var b = B.SkillRoll;

        var cache = new Dictionary<string, SkillRollResult>();

        for (int solIndex = 0; solIndex < phase.Solutions.Count; solIndex++)
        {
            var sol = phase.Solutions[solIndex];
            if (sol.Skills == null || sol.Skills.Count == 0) continue;

            bool allPassed = true;

            foreach (var kv in sol.Skills)
            {
                string skillId = kv.Key;
                double rawDc = kv.Value;

                string cacheKey = $"{skillId}|{rawDc:F4}";

                if (!cache.TryGetValue(cacheKey, out var roll))
                {
                    double required = (rawDc + extraDc) * refMax;

                    double bestSkill = 0;
                    AdventurerModel bestAdv = null;

                    foreach (var a in party)
                    {
                        if (a.HasStatus(StatusIds.Dead)) continue;

                        double v = SkillValue(a, skillId);
                        if (v > bestSkill)
                        {
                            bestSkill = v;
                            bestAdv = a;
                        }
                    }

                    double ratio = required > 0 ? bestSkill / required : 0;
                    double chance = Math.Clamp(
                        b.Base + (ratio - 1.0) * b.PerRatio,
                        b.Min, b.Max);

                    double rollValue = rng.NextDouble() * 100.0;
                    bool passed = rollValue < chance;

                    roll = new SkillRollResult
                    {
                        SkillId = skillId,
                        BestAdvName = bestAdv?.FullName ?? "-",
                        SkillValue = bestSkill,
                        Required = required,
                        Ratio = ratio,
                        Chance = chance,
                        Roll = rollValue,
                        Passed = passed,
                        SolutionIndex = solIndex,
                    };

                    cache[cacheKey] = roll;
                    outcome.Rolls.Add(roll);
                }

                if (!roll.Passed)
                {
                    allPassed = false;
                    break;
                }
            }

            if (allPassed)
            {
                outcome.Passed = true;
                return outcome;
            }
        }

        return outcome;
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

    public static double Contribution(AdventurerModel a, List<PhaseResult> results, int codeLevel)
    {
        if (a == null || results == null || results.Count == 0) return 0;

        double baseScore = B.Experience?.ContributionBaseScore ?? 0.2;
        double refMax = ReferenceMaxStatQuest(codeLevel);
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
                    double required = kv.Value * refMax;
                    if (required <= 0) { worstInSolution = 0; break; }

                    double norm = SkillValue(a, kv.Key);
                    double relative = norm / required;
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
        List<PhaseResult> results,
        int codeLevel)
    {
        var result = new Dictionary<AdventurerModel, double>();
        if (party == null || party.Count == 0) return result;

        double total = 0;
        var scores = new double[party.Count];

        for (int i = 0; i < party.Count; i++)
        {
            scores[i] = Contribution(party[i], results, codeLevel);
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

    // === Ожидаемые броски (без rng, для UI / preview) ===

    public static List<SkillRollResult> ExpectedRolls(
        QuestPhaseInstance phase,
        IReadOnlyList<AdventurerModel> party,
        int codeLevel)
    {
        var bestSolutionRolls = new List<SkillRollResult>();
        if (phase?.Solutions == null || party == null) return bestSolutionRolls;

        double refMax = ReferenceMaxStatQuest(codeLevel);
        var b = B.SkillRoll;

        double bestSolutionMinChance = -1;

        for (int solIndex = 0; solIndex < phase.Solutions.Count; solIndex++)
        {
            var sol = phase.Solutions[solIndex];
            if (sol.Skills == null || sol.Skills.Count == 0) continue;

            var solutionRolls = new List<SkillRollResult>();
            double solutionMinChance = double.MaxValue;

            foreach (var kv in sol.Skills)
            {
                string skillId = kv.Key;
                double required = kv.Value * refMax;

                double bestSkill = 0;
                AdventurerModel bestAdv = null;

                foreach (var a in party)
                {
                    if (a.HasStatus(StatusIds.Dead)) continue;
                    double v = SkillValue(a, skillId);
                    if (v > bestSkill) { bestSkill = v; bestAdv = a; }
                }

                double ratio = required > 0 ? bestSkill / required : 0;
                double chance = Math.Clamp(
                    b.Base + (ratio - 1.0) * b.PerRatio,
                    b.Min, b.Max);

                if (chance < solutionMinChance) solutionMinChance = chance;

                solutionRolls.Add(new SkillRollResult
                {
                    SkillId = skillId,
                    BestAdvName = bestAdv?.FullName ?? "-",
                    SkillValue = bestSkill,
                    Required = required,
                    Ratio = ratio,
                    Chance = chance,
                    Roll = 0,
                    Passed = chance >= 50,
                    SolutionIndex = solIndex,
                });
            }

            if (solutionMinChance > bestSolutionMinChance)
            {
                bestSolutionMinChance = solutionMinChance;
                bestSolutionRolls = solutionRolls;
            }
        }

        return bestSolutionRolls;
    }

    public static double ComputeEscapeChance(AdventurerModel a, CreatureInfo creature, int codeLevel)
    {
        var b = B;

        double baseEscape = b.EscapeBase;
        double combatDcDelta = ExtractCombatDcDelta(creature);
        double creatureMod = -combatDcDelta * 100;

        double refMax = ReferenceMaxStatQuest(codeLevel);

        double str = EffectiveStat(a, StatIds.Strength) / refMax;
        double dex = EffectiveStat(a, StatIds.Dexterity) / refMax;
        double end = EffectiveStat(a, StatIds.Endurance) / refMax;
        double wis = EffectiveStat(a, StatIds.Wisdom) / refMax;

        double best = Math.Max(Math.Max(str, dex), Math.Max(end * b.EscapeEndWeight, wis * b.EscapeWisWeight));
        double statBonus = Math.Min(b.EscapeStatCap, best * b.EscapeStatK);

        double final = baseEscape + creatureMod + statBonus;
        return Math.Clamp(final, 0, b.EscapeStatCap);
    }

    private static double ExtractCombatDcDelta(CreatureInfo creature)
    {
        return 0;
    }
}