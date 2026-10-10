using System.Collections.Generic;

namespace AdventurersGuild.Domain.Adventurers;

public class AdventurerTemplateInfo
{
    public string Id { get; set; }
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }
}

public class AdventurerTemplateDatabase
{
    public List<AdventurerTemplateInfo> Templates { get; set; } = new();
}
