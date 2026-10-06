using System.Collections.Generic;

public class QuestTemplate
{
    public string Id { get; set; }
    public LocalizedString Name { get; set; }
    public LocalizedString Description { get; set; }
    public string Type { get; set; }
    public string Tier { get; set; }
    public List<string> Phases { get; set; }
    public int TravelDays { get; set; }
    public LevelRangeInfo LevelRange { get; set; } = new();
    public List<string> Creatures { get; set; }
    public List<string> Locations { get; set; }
    public int BaseGold { get; set; }
    public int BaseGlory { get; set; }
}

public class QuestTemplateDatabase
{
    public List<QuestTemplate> Templates { get; set; } = new();
}