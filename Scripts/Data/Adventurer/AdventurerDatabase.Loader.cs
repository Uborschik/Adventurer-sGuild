using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Core;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Names;

namespace AdventurersGuild.Data.Adventurer;

public static partial class AdventurerDatabase
{
    private const string DataPath = "res://Resources/Data/Adventurer/";

    public static void Load()
    {
        LoadClasses();
        LoadRaces();
        LoadStats();
        LoadStatuses();
        LoadTemplates();
        LoadNames();

        BuildSkillIndex();

        Validate();
        ValidateNames();
        ValidateSkills();
    }

    private static void LoadClasses()
    {
        Classes = JsonLoader.Load<AdventurerClassDatabase>(DataPath + "AdventurerClasses.json")
            .Classes.ToDictionary(c => c.Id);
    }

    private static void LoadRaces()
    {
        Races = JsonLoader.Load<AdventurerRaceDatabase>(DataPath + "AdventurerRaces.json")
            .Races.ToDictionary(r => r.Id);
    }

    private static void LoadStats()
    {
        Stats = JsonLoader.Load<AdventurerStatDatabase>(DataPath + "AdventurerStats.json")
            .Stats.ToDictionary(s => s.Id);
    }

    private static void LoadStatuses()
    {
        Statuses = JsonLoader.Load<AdventurerStatusDatabase>(DataPath + "AdventurerStatuses.json")
            .Statuses.ToDictionary(s => s.Id);
    }

    private static void LoadTemplates()
    {
        Templates = JsonLoader.Load<AdventurerTemplateDatabase>(DataPath + "AdventurerTemplates.json")
            .Templates;
    }

    private static void LoadNames()
    {
        Names = JsonLoader.Load<NameDatabase>(DataPath + "AdventurerNames.json");
    }

    private static void BuildSkillIndex()
    {
        skillToStat = new Dictionary<string, string>();
        skillsById = new Dictionary<string, SkillInfo>();

        foreach (var stat in Stats.Values)
        {
            if (stat.Skills == null || stat.Skills.Count == 0)
            {
                Log.Warn($"[AdventurerDB] Стат '{stat.Id}': нет навыков");
                continue;
            }

            foreach (var skill in stat.Skills)
            {
                if (skill == null || string.IsNullOrEmpty(skill.Id)) continue;

                if (skillToStat.ContainsKey(skill.Id))
                {
                    Log.Warn($"[AdventurerDB] Навык '{skill.Id}' привязан к нескольким статам: " +
                             $"'{skillToStat[skill.Id]}' и '{stat.Id}'");
                    continue;
                }

                skillToStat[skill.Id] = stat.Id;
                skillsById[skill.Id] = skill;
            }
        }
    }

    public static void Install(
        IReadOnlyDictionary<string, AdventurerClassInfo> classes = null,
        IReadOnlyDictionary<string, AdventurerRaceInfo> races = null,
        IReadOnlyDictionary<string, AdventurerStatInfo> stats = null,
        IReadOnlyDictionary<string, AdventurerStatusInfo> statuses = null,
        IReadOnlyList<AdventurerTemplateInfo> templates = null,
        NameDatabase names = null)
    {
        if (classes != null) Classes = classes;
        if (races != null) Races = races;
        if (stats != null) Stats = stats;
        if (statuses != null) Statuses = statuses;
        if (templates != null) Templates = templates;
        if (names != null) Names = names;

        BuildSkillIndex();
    }
}