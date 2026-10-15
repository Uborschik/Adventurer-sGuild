using System.Collections.Generic;

namespace AdventurersGuild.Domain.Adventurers;

public class UnitTemplateInfo
{
    public string Id { get; set; }
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }
}

public class AdventurerTemplateDatabase
{
    public List<UnitTemplateInfo> Templates { get; set; } = new();
}
