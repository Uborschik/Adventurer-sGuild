using System;
using System.Collections.Generic;

public static class StatGrowth
{
    private const double DefaultTertiaryWeight = 0.20;

    public static Dictionary<string, int> AtLevel(string classId, string raceId, int level = 1)
    {
        var cls = AdventurerDatabase.GetClass(classId);
        var race = AdventurerDatabase.GetRace(raceId);

        if (cls == null || race == null)
            return new Dictionary<string, int>();

        var p = AdventurerBalance.Active;
        int levelsAboveOne = Math.Max(0, level - 1);

        var result = new Dictionary<string, int>();

        foreach (var statId in StatIds.All)
        {
            double weight = DefaultTertiaryWeight;

            if (cls.GrowthWeights != null
                && cls.GrowthWeights.TryGetValue(statId, out var w))
                weight = w;

            double value = p.StatBaseValue + (p.StatStartPool + levelsAboveOne * p.StatBaseSlope) * weight;

            if (race.StatBonuses != null
                && race.StatBonuses.TryGetValue(statId, out int bonus))
                value += bonus;

            if (statId == StatIds.Endurance
                && Math.Abs(race.ResilienceMultiplier - 1.0) > 1e-6)
                value *= race.ResilienceMultiplier;

            result[statId] = (int)Math.Round(value);
        }

        return result;
    }
}