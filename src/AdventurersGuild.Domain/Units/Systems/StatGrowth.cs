namespace AdventurersGuild.Domain.Units;

using System;
using System.Collections.Generic;

public static class StatGrowth
{
    private const double defaultTertiaryWeight = 0.20;

    public static Dictionary<string, double> AtLevel(string classId, string speciesId, int level = 1)
    {
        var cls = UnitDatabase.GetClass(classId);
        var species = UnitDatabase.GetSpecies(speciesId);

        if (cls == null || species == null)
            return new Dictionary<string, double>();

        var p = LevelBalance.Active;
        int levelsAboveOne = Math.Max(0, level - 1);

        var result = new Dictionary<string, double>();

        foreach (var statId in StatIds.All)
        {
            // 1. Вес из вида (Species)
            double weight = defaultTertiaryWeight;
            if (species.GrowthWeights != null && species.GrowthWeights.TryGetValue(statId, out var w))
                weight = w;

            // 2. База
            double value = p.StatBaseValue
                         + (p.StatStartPool + levelsAboveOne * p.StatBaseSlope) * weight;

            // 3. Бонусы вида (Species)
            if (species.StatBonuses != null && species.StatBonuses.TryGetValue(statId, out int bonus))
                value += bonus;

            // 4. Модификаторы класса (Class)
            if (cls.StatModifiers != null && cls.StatModifiers.TryGetValue(statId, out double modifier))
                value *= modifier;

            result[statId] = value;
        }

        return result;
    }
}