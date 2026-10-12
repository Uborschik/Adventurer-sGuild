using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

namespace AdventurersGuild.Domain.Adventurers;

public class AdventurerStatInfo
{
    public string Id { get; set; }
    public LocalizedString Name { get; set; }
    public LocalizedString Description { get; set; }
    public List<SkillInfo> Skills { get; set; } = new();
}

public class SkillInfo
{
    public string Id { get; set; }
    public LocalizedString Name { get; set; }
    public double StatModifier { get; set; } = 1.0;
    public Dictionary<string, double> ClassModifiers { get; set; } = new();
}

public class AdventurerStatDatabase
{
    public List<AdventurerStatInfo> Stats { get; set; } = new();
}