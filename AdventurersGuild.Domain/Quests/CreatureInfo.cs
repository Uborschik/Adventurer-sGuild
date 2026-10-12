namespace AdventurersGuild.Domain.Quests;

using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

public class CreatureInfo
{
    public string Id { get; set; }
    public LocalizedNoun Name { get; set; }
    public int MinLvl { get; set; } = 1;
    public int MaxLvl { get; set; } = 60;
    public Dictionary<string, double> GrowthWeights { get; set; } = new();
    public List<CreatureContribution> Contributions { get; set; } = new();
}

public class CreatureDatabase
{
    public List<CreatureInfo> Creatures { get; set; } = new();
}