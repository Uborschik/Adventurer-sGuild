using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

namespace AdventurersGuild.Domain.Adventurers;

public class SkillInfo
{
    public string Id { get; set; }
    public string StatId { get; set; }
    public LocalizedString Name { get; set; }
    public Dictionary<string, double> ClassModifiers { get; set; } = new();
}

public class AdventurerSkillDatabase
{
    public List<SkillInfo> Skills { get; set; } = new();
}