namespace AdventurersGuild.Data.Adventurer;

using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Names;

public static class AdventurerLoader
{
    private const string dataPath = "Data/Adventurer/";

    public static void Load()
    {
        Dictionary<string, AdventurerClassInfo> classes = LoadClasses();
        Dictionary<string, AdventurerRaceInfo> races = LoadRaces();
        Dictionary<string, AdventurerStatInfo> stats = LoadStats();
        Dictionary<string, SkillInfo> skills = LoadSkills();
        Dictionary<string, AdventurerStatusInfo> statuses = LoadStatuses();
        IReadOnlyList<AdventurerTemplateInfo> templates = LoadTemplates();
        NameDatabase names = LoadNames();

        AdventurerDatabase.Install(classes, races, stats, skills, statuses, templates, names);

        AdventurerValidator.Validate();
        AdventurerValidator.ValidateNames();
        AdventurerValidator.ValidateSkills();
    }

    private static Dictionary<string, AdventurerClassInfo> LoadClasses()
        => JsonLoader.Load<AdventurerClassDatabase>(dataPath + "AdventurerClasses.json")
            .Classes.ToDictionary(c => c.Id);

    private static Dictionary<string, AdventurerRaceInfo> LoadRaces()
        => JsonLoader.Load<AdventurerRaceDatabase>(dataPath + "AdventurerRaces.json")
            .Races.ToDictionary(r => r.Id);

    private static Dictionary<string, AdventurerStatInfo> LoadStats()
        => JsonLoader.Load<AdventurerStatDatabase>(dataPath + "Stats.json")
            .Stats.ToDictionary(s => s.Id);

    private static Dictionary<string, SkillInfo> LoadSkills()
        => JsonLoader.Load<AdventurerSkillDatabase>(dataPath + "Skills.json")
            .Skills.ToDictionary(s => s.Id);

    private static Dictionary<string, AdventurerStatusInfo> LoadStatuses()
        => JsonLoader.Load<AdventurerStatusDatabase>(dataPath + "AdventurerStatuses.json")
            .Statuses.ToDictionary(s => s.Id);

    private static IReadOnlyList<AdventurerTemplateInfo> LoadTemplates()
        => JsonLoader.Load<AdventurerTemplateDatabase>(dataPath + "AdventurerTemplates.json")
            .Templates;

    private static NameDatabase LoadNames()
        => JsonLoader.Load<NameDatabase>(dataPath + "AdventurerNames.json");
}