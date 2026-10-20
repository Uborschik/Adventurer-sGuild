namespace AdventurersGuild.Data.Units;

using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Core;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Domain.Units;
using AdventurersGuild.Domain.Names;

public static class UnitLoader
{
    private const string dataPath = "Data/Units/";

    public static void Load()
    {
        LoadLevelBalance();

        Dictionary<string, ClassInfo> classes = LoadClasses();
        Dictionary<string, SpeciesInfo> species = LoadSpecies();
        Dictionary<string, StatInfo> stats = LoadStats();
        Dictionary<string, SkillInfo> skills = LoadSkills();
        Dictionary<string, StatusInfo> statuses = LoadStatuses();
        NameDatabase names = LoadNames();

        UnitDatabase.Install(classes, species, stats, skills, statuses, names);


    }

    private static void LoadLevelBalance()
    {
        var p = JsonLoader.Load<LevelBalanceProfile>(dataPath + "LevelBalance.json");

        if (p.MaxLevel < 1) p.MaxLevel = 60;
        if (p.ExpCapLvl1 < 1) p.ExpCapLvl1 = 45;

        switch (p.ExpCapScaling)
        {
            case "flat":
            case "linear":
            case "polynomial":
            case "quadratic":
            case "exponential":
                break;
            default:
                Log.Error($"[LevelBalance] неизвестный scaling '{p.ExpCapScaling}', использую polynomial");
                p.ExpCapScaling = "polynomial";
                break;
        }

        if (p.ExpCapScaling == "polynomial" && p.ExpCapAcceleration <= 0)
            p.ExpCapAcceleration = 0.22;

        if (p.StatBaseValue < 0) p.StatBaseValue = 0.0;
        if (p.StatStartPool <= 0) p.StatStartPool = 16.0;
        if (p.StatBaseSlope <= 0) p.StatBaseSlope = 2.5;

        LevelBalance.SetActiveProfile(p);
        Log.Info($"[LevelBalance] загружен: maxLevel={p.MaxLevel}, expCapLvl1={p.ExpCapLvl1}");
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