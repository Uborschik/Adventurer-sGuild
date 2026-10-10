namespace AdventurersGuild.Application.Quests;

using System.Collections.Generic;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Quests;

public class CreatureStats
{
    private readonly Dictionary<string, double> values = new();

    public CreatureStats(CreatureInfo creature, int codeLevel, AdventurerBalanceProfile balance)
    {
        double refMax = balance.StatStartPool + (codeLevel - 1) * balance.StatBaseSlope;

        foreach (var statId in StatIds.All)
        {
            double weight = creature.GrowthWeights != null
                && creature.GrowthWeights.TryGetValue(statId, out var w) ? w : 0;

            values[statId] = balance.StatBaseValue + refMax * weight;
        }
    }

    public double Get(string statId) => values.TryGetValue(statId, out var v) ? v : 0;
}