namespace AdventurersGuild.Data.Adventurer;

using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Names;

public static class UnitLoader
{
    private const string dataPath = "Data/Adventurer/";

    public static void Load()
    {
        Dictionary<string, ClassInfo> classes = LoadClasses();
        Dictionary<string, RaceInfo> races = LoadRaces();
        Dictionary<string, StatInfo> stats = LoadStats();
        Dictionary<string, SkillInfo> skills = LoadSkills();
        Dictionary<string, StatusInfo> statuses = LoadStatuses();
        IReadOnlyList<UnitTemplateInfo> templates = LoadTemplates();
        NameDatabase names = LoadNames();

        UnitDatabase.Install(classes, races, stats, skills, statuses, templates, names);

        UnitValidator.Validate();
        UnitValidator.ValidateNames();
        UnitValidator.ValidateSkills();
    }

    private static Dictionary<string, ClassInfo> LoadClasses()
        => JsonLoader.Load<AdventurerClassDatabase>(dataPath + "AdventurerClasses.json")
            .Classes.ToDictionary(c => c.Id);

    private static Dictionary<string, RaceInfo> LoadRaces()
        => JsonLoader.Load<AdventurerRaceDatabase>(dataPath + "AdventurerRaces.json")
            .Races.ToDictionary(r => r.Id);

    private static Dictionary<string, StatInfo> LoadStats()
        => JsonLoader.Load<AdventurerStatDatabase>(dataPath + "Stats.json")
            .Stats.ToDictionary(s => s.Id);

    private static Dictionary<string, SkillInfo> LoadSkills()
        => JsonLoader.Load<AdventurerSkillDatabase>(dataPath + "Skills.json")
            .Skills.ToDictionary(s => s.Id);

    private static Dictionary<string, StatusInfo> LoadStatuses()
        => JsonLoader.Load<AdventurerStatusDatabase>(dataPath + "AdventurerStatuses.json")
            .Statuses.ToDictionary(s => s.Id);

    private static IReadOnlyList<UnitTemplateInfo> LoadTemplates()
        => JsonLoader.Load<AdventurerTemplateDatabase>(dataPath + "AdventurerTemplates.json")
            .Templates;

    private static NameDatabase LoadNames()
        => JsonLoader.Load<NameDatabase>(dataPath + "AdventurerNames.json");
}