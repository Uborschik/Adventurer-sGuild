namespace AdventurersGuild.Data.Adventurer;

using System.Linq;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Names;

public static class AdventurerLoader
{
    private const string DataPath = "res://Resources/Data/Adventurer/";

    public static void Load()
    {
        var classes = LoadClasses();
        var races = LoadRaces();
        var stats = LoadStats();
        var statuses = LoadStatuses();
        var templates = LoadTemplates();
        var names = LoadNames();

        AdventurerDatabase.Install(classes, races, stats, statuses, templates, names);

        AdventurerValidator.Validate();
        AdventurerValidator.ValidateNames();
        AdventurerValidator.ValidateSkills();
    }

    private static System.Collections.Generic.IReadOnlyDictionary<string, AdventurerClassInfo> LoadClasses()
        => JsonLoader.Load<AdventurerClassDatabase>(DataPath + "AdventurerClasses.json")
            .Classes.ToDictionary(c => c.Id);

    private static System.Collections.Generic.IReadOnlyDictionary<string, AdventurerRaceInfo> LoadRaces()
        => JsonLoader.Load<AdventurerRaceDatabase>(DataPath + "AdventurerRaces.json")
            .Races.ToDictionary(r => r.Id);

    private static System.Collections.Generic.IReadOnlyDictionary<string, AdventurerStatInfo> LoadStats()
        => JsonLoader.Load<AdventurerStatDatabase>(DataPath + "AdventurerStats.json")
            .Stats.ToDictionary(s => s.Id);

    private static System.Collections.Generic.IReadOnlyDictionary<string, AdventurerStatusInfo> LoadStatuses()
        => JsonLoader.Load<AdventurerStatusDatabase>(DataPath + "AdventurerStatuses.json")
            .Statuses.ToDictionary(s => s.Id);

    private static System.Collections.Generic.IReadOnlyList<AdventurerTemplateInfo> LoadTemplates()
        => JsonLoader.Load<AdventurerTemplateDatabase>(DataPath + "AdventurerTemplates.json")
            .Templates;

    private static NameDatabase LoadNames()
        => JsonLoader.Load<NameDatabase>(DataPath + "AdventurerNames.json");
}