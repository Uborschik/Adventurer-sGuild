using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

namespace AdventurersGuild.Domain.Quests;

public class QuestTemplate
{
    public string Id { get; set; }
    public LocalizedString Name { get; set; }
    public LocalizedString Description { get; set; }
    public string Type { get; set; }
    public double DifficultyModifier { get; set; }
    public List<string> Requires { get; set; } = new();
    public List<string> Creatures { get; set; } = new();
    public List<string> Locations { get; set; } = new();
    public int TravelDays { get; set; }
    public LevelRangeInfo LevelRange { get; set; } = new();
    public int BaseGold { get; set; }
    public int BaseGlory { get; set; }
}

public class QuestTemplateDatabase
{
    public List<QuestTemplate> Templates { get; set; } = new();
}