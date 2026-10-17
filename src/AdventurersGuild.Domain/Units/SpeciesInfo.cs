namespace AdventurersGuild.Domain.Adventurers;

using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

public class SpeciesInfo
{
    public string Id { get; set; }
    public LocalizedNoun Name { get; set; }
    public int Population { get; set; } = 10;
    public Dictionary<string, int> StatBonuses { get; set; } = [];
    public Dictionary<string, double> GrowthWeights { get; set; } = [];
    public List<SpeciesClassInfo> Classes { get; set; } = [];
}

public class SpeciesClassInfo
{
    public string ClassId { get; set; }
    public int Weight { get; set; }
    public int MinLvl { get; set; }
    public int MaxLvl { get; set; }
}

public class SpeciesDatabase
{
    public List<SpeciesInfo> Species { get; set; } = [];
}