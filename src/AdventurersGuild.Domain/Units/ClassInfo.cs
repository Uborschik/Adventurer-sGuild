namespace AdventurersGuild.Domain.Adventurers;

using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

public class ClassInfo
{
    public string Id { get; set; }
    public LocalizedString Name { get; set; }
    public string PrimaryStat { get; set; }
    public string PrimarySkill { get; set; }
    public string SecondaryStat { get; set; }
    public string SecondarySkill { get; set; }

    public int ClassCost { get; set; } = 1;
    public Dictionary<string, double> StatModifiers { get; set; } = [];
}

public class ClassDatabase
{
    public List<ClassInfo> Classes { get; set; } = [];
}