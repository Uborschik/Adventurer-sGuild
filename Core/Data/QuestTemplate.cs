using System.Collections.Generic;

public class QuestTemplate
{
    public string Id { get; set; }
    public LocalizedString Name { get; set; }
    public LocalizedString Description { get; set; }
    public string Type { get; set; }
    public int BaseGold { get; set; }
    public int BaseGlory { get; set; }
    public int DurationDays { get; set; }
    public LevelRangeInfo LevelRange { get; set; } = new();
    public List<string> Creatures { get; set; }
    public List<string> Locations { get; set; }

    // новые поля
    public List<string> Tags { get; set; }
    public string Tier { get; set; }
    public Dictionary<string, double> Weights { get; set; }
    public int RecommendedPartySize { get; set; } = 1;
}

public class QuestTemplateDatabase
{
    public List<QuestTemplate> Templates { get; set; } = new();
}