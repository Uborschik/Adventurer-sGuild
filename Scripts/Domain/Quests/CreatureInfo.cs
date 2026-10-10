using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

namespace AdventurersGuild.Domain.Quests;

public class CreatureInfo
{
    public string Id { get; set; }
    public LocalizedNoun Name { get; set; }
    public int MinLvl { get; set; } = 1;
    public int MaxLvl { get; set; } = 60;
    public Dictionary<string, double> GrowthWeights { get; set; } = new();
    public List<PhasePromise> Promises { get; set; } = new();
}

public class PhasePromise
{
    public string Phase { get; set; }
    public int DurationMinutes { get; set; }
    public int ExpReward { get; set; }
}

public class CreatureDatabase
{
    public List<CreatureInfo> Creatures { get; set; } = new();
}