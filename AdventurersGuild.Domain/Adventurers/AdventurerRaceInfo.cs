using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

namespace AdventurersGuild.Domain.Adventurers;

public class AdventurerRaceInfo
{
    public string Id { get; set; }
    public LocalizedString Name { get; set; }
    public Dictionary<string, int> StatBonuses { get; set; }
    public double ResilienceMultiplier { get; set; } = 1.0;
    public RaceWeightInfo Weight { get; set; }
}

public class RaceWeightInfo
{
    public int Population { get; set; }
    public Dictionary<string, int> Classes { get; set; }
}

public class AdventurerRaceDatabase
{
    public List<AdventurerRaceInfo> Races { get; set; } = new();
}