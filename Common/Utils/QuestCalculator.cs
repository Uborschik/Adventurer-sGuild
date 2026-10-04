using Godot;
using System;
using System.Collections.Generic;

public enum QuestTier { Easy, Normal, Hard }

public static class QuestCalculator
{
    private static QuestBalanceProfile B => QuestBalance.Active;

    public static double ReferenceMaxStat(AdventurerModel a)
        => B.ReferenceStatAtLevel1 + (a.Level.Number - 1);

    public static double NormalizedStat(AdventurerModel a, string statId)
    {
        if (a == null || a.Level.Number <= 0 || a.Stats == null) return 0;
        if (!a.Stats.TryGetValue(statId, out var v)) return 0;

        double maxStat = ReferenceMaxStat(a);
        if (maxStat <= 0) return 0;

        double woundedFactor = a.HasStatus(StatusIds.Wounded) ? 0.5 : 1.0;
        return woundedFactor * v / maxStat;
    }

    public static double Power(AdventurerModel a, IReadOnlyDictionary<string, double> weights)
    {
        if (a == null || a.Level.Number <= 0) return 0;
        if (a.Stats == null) return 0;
        if (weights == null) return 0;

        var woundedFactor = a.HasStatus(StatusIds.Wounded) ? 0.5 : 1.0;
        var maxStat = ReferenceMaxStat(a);

        if (maxStat <= 0) return 0;

        double statScore = 0;

        foreach (var statId in StatIds.All)
        {
            if (!weights.TryGetValue(statId, out var w) || w <= 0) continue;

            int value = a.Stats.TryGetValue(statId, out var s) ? s : 0;
            statScore += w * (value / maxStat);
        }

        return a.Level.Number * statScore * woundedFactor;
    }

    public static double TeamPower(IEnumerable<AdventurerModel> party, IReadOnlyDictionary<string, double> weights)
    {
        if (party == null || weights == null) return 0;

        var list = party as IList<AdventurerModel> ?? new List<AdventurerModel>(party);
        double sum = 0;

        foreach (var a in list) sum += Power(a, weights);

        if (list.Count == 0) return sum;

        var covered = 0;

        foreach (var statId in StatIds.All)
        {
            if (!weights.TryGetValue(statId, out var w) || w <= 0) continue;

            foreach (var a in list)
            {
                if (NormalizedStat(a, statId) >= B.CoverageThreshold)
                {
                    covered++;
                    break;
                }
            }
        }

        return sum + covered * B.CoverageBonusPerStat;
    }

    public static double Threshold(int needAdvCount, IEnumerable<AdventurerModel> party, IReadOnlyDictionary<string, double> weights, QuestTier tier)
    {
        if (needAdvCount <= 0) return 0;

        double teamPower = TeamPower(party, weights);

        double missing = Math.Max(0, needAdvCount - teamPower);
        double overflow = Math.Max(0, teamPower - needAdvCount);
        double effectiveCount = Math.Min(teamPower, needAdvCount);

        double effectivePenalty = Math.Max(0, B.BasePenalty - overflow * B.BasePenalty / needAdvCount);
        double bonus = (100.0 - effectivePenalty) * effectiveCount / needAdvCount;
        double penalty = 2.0 * missing * missing;

        return 101.0 + B.TierBonus.Get(tier) + penalty - bonus;
    }

    public static double Margin(int needAdvCount, IEnumerable<AdventurerModel> party, IReadOnlyDictionary<string, double> weights, QuestTier tier, Random rng)
    {
        if (needAdvCount <= 0) return 100;
        if (rng == null) throw new ArgumentNullException(nameof(rng));

        double threshold = Threshold(needAdvCount, party, weights, tier);
        int roll = rng.Next(1, 101);

        return roll - threshold;
    }

    public static bool IsSuccess(int needAdvCount, IEnumerable<AdventurerModel> party, IReadOnlyDictionary<string, double> weights, QuestTier tier, Random rng)
        => Margin(needAdvCount, party, weights, tier, rng) >= 0;

    public static double ExpBonus(int questLevel, QuestTier tier)
    {
        var e = QuestBalance.Active.Experience;
        if (e == null) return 0;

        double tierMod = e.TierModifiers.Get(tier);
        return questLevel * (e.ExpBonusLvl1 + tierMod);
    }

    public static double TotalExpPool(int questLevel, QuestTier tier, QuestGradeRole role)
    {
        var e = QuestBalance.Active.Experience;
        if (e == null) return 0;

        double roleMul = e.RoleMultipliers.Get(role);
        return ExpBonus(questLevel, tier) * roleMul;
    }

    public static double Contribution(AdventurerModel a, IReadOnlyDictionary<string, double> weights)
    {
        if (a == null || weights == null) return 0;

        var e = QuestBalance.Active.Experience;
        double baseScore = e?.ContributionBaseScore ?? 0.2;

        double score = 0;
        foreach (var statId in StatIds.All)
        {
            if (!weights.TryGetValue(statId, out var w) || w <= 0) continue;
            score += w * NormalizedStat(a, statId);
        }

        return baseScore + score;
    }

    public static Dictionary<AdventurerModel, double> ContributionShares(IReadOnlyList<AdventurerModel> party, IReadOnlyDictionary<string, double> weights)
    {
        var result = new Dictionary<AdventurerModel, double>();
        if (party == null || party.Count == 0) return result;

        double total = 0;
        var scores = new double[party.Count];

        for (int i = 0; i < party.Count; i++)
        {
            scores[i] = Contribution(party[i], weights);
            total += scores[i];
        }

        if (total <= 0)
        {
            // делим поровну
            double even = 1.0 / party.Count;
            foreach (var a in party) result[a] = even;
            return result;
        }

        for (int i = 0; i < party.Count; i++)
            result[party[i]] = scores[i] / total;

        return result;
    }
}