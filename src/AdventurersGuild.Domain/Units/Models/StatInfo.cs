namespace AdventurersGuild.Domain.Units;

using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

public class StatInfo
{
    public string Id { get; set; } = string.Empty;
    public LocalizedString Name { get; set; }
    public LocalizedString Description { get; set; }

    public string PrimaryStat { get; set; }
    public string PrimarySkill { get; set; }
    public string SecondaryStat { get; set; }
    public string SecondarySkill { get; set; }

    public int ClassCost { get; set; } = 1;
    public double HpMultiplier { get; set; } = 1.0;

    public Dictionary<string, double> StatModifiers { get; set; } = new();
}

public class AdventurerStatDatabase
{
    public List<StatInfo> Stats { get; set; } = [];
}