namespace AdventurersGuild.Data.Adventurer;

using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Names;

public static class UnitLoader
{
    private const string dataPath = "Data/Units/";

    public static void Load()
    {
        Dictionary<string, ClassInfo> classes = LoadClasses();
        Dictionary<string, SpeciesInfo> species = LoadSpecies();
        Dictionary<string, StatInfo> stats = LoadStats();
        Dictionary<string, SkillInfo> skills = LoadSkills();
        Dictionary<string, StatusInfo> statuses = LoadStatuses();
        GameRulesInfo gameRules = JsonLoader.Load<GameRulesInfo>(dataPath + "GameRules.json");
        NameDatabase names = LoadNames();

        UnitDatabase.Install(gameRules, classes, species, stats, skills, statuses, names);

        UnitValidator.Validate();
        UnitValidator.ValidateNames();
        UnitValidator.ValidateSkills();
    }

    private static Dictionary<string, ClassInfo> LoadClasses()
    {
        var humanoidClasses = JsonLoader.Load<ClassDatabase>(dataPath + "HumanoidClasses.json").Classes;
        var beastClasses = JsonLoader.Load<ClassDatabase>(dataPath + "BeastClasses.json").Classes;

        return humanoidClasses.Concat(beastClasses).ToDictionary(c => c.Id);
    }

    private static Dictionary<string, SpeciesInfo> LoadSpecies()
    {
        var humanoidSpecies = JsonLoader.Load<SpeciesDatabase>(dataPath + "HumanoidSpecies.json").Species;
        var beastSpecies = JsonLoader.Load<SpeciesDatabase>(dataPath + "BeastSpecies.json").Species;

        return humanoidSpecies.Concat(beastSpecies).ToDictionary(s => s.Id);
    }

    private static Dictionary<string, StatInfo> LoadStats()
        => JsonLoader.Load<AdventurerStatDatabase>(dataPath + "Stats.json")
            .Stats.ToDictionary(s => s.Id);

    private static Dictionary<string, SkillInfo> LoadSkills()
        => JsonLoader.Load<AdventurerSkillDatabase>(dataPath + "Skills.json")
            .Skills.ToDictionary(s => s.Id);

    private static Dictionary<string, StatusInfo> LoadStatuses()
        => JsonLoader.Load<AdventurerStatusDatabase>(dataPath + "Statuses.json")
            .Statuses.ToDictionary(s => s.Id);

    private static NameDatabase LoadNames()
        => JsonLoader.Load<NameDatabase>(dataPath + "Names.json");
}