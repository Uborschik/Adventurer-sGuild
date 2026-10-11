namespace AdventurersGuild.Domain.Adventurers;

using System.Collections.Generic;
using AdventurersGuild.Core;
using AdventurersGuild.Domain.Names;

public static class AdventurerDatabase
{
    public static IReadOnlyDictionary<string, AdventurerClassInfo> Classes { get; private set; }
    public static IReadOnlyDictionary<string, AdventurerRaceInfo> Races { get; private set; }
    public static IReadOnlyDictionary<string, AdventurerStatInfo> Stats { get; private set; }
    public static IReadOnlyDictionary<string, AdventurerStatusInfo> Statuses { get; private set; }
    public static IReadOnlyList<AdventurerTemplateInfo> Templates { get; private set; }
    public static NameDatabase Names { get; private set; }

    private static Dictionary<string, string> skillToStat;
    private static Dictionary<string, SkillInfo> skillsById;

    public static int SkillCount => skillToStat?.Count ?? 0;

    // === Install ===

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

    private static void BuildSkillIndex()
    {
        skillToStat = new Dictionary<string, string>();
        skillsById = new Dictionary<string, SkillInfo>();

        if (Stats == null) return;

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

    // === Lookups ===

    public static AdventurerClassInfo GetClass(string id) => Lookup(Classes, id, "класс");
    public static AdventurerRaceInfo GetRace(string id) => Lookup(Races, id, "раса");
    public static AdventurerStatInfo GetStat(string id) => Lookup(Stats, id, "стат");
    public static AdventurerStatusInfo GetStatus(string id) => Lookup(Statuses, id, "статус");

    public static string StatForSkill(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return null;
        return skillToStat != null && skillToStat.TryGetValue(skillId, out var s) ? s : null;
    }

    public static SkillInfo GetSkill(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return null;
        return skillsById != null && skillsById.TryGetValue(skillId, out var s) ? s : null;
    }

    public static bool IsValidSkill(string skillId)
        => !string.IsNullOrEmpty(skillId)
           && skillToStat != null
           && skillToStat.ContainsKey(skillId);

    public static string ClassName(string id)
    {
        var c = GetClass(id);
        return c?.Name.Get(Loc.Language) ?? id;
    }

    public static string RaceName(string id)
    {
        var r = GetRace(id);
        return r?.Name.Get(Loc.Language) ?? id;
    }

    public static string StatName(string id)
    {
        var s = GetStat(id);
        return s?.Name.Get(Loc.Language) ?? id;
    }

    public static string StatDescription(string id)
    {
        var s = GetStat(id);
        return s?.Description.Get(Loc.Language) ?? "";
    }

    public static string StatusName(string id)
    {
        var s = GetStatus(id);
        return s?.Name.Get(Loc.Language) ?? id;
    }

    public static bool IsFree(AdventurerModel a)
    {
        if (a == null) return false;
        var info = GetStatus(a.StatusId);
        return info == null || !info.BlocksAssignment;
    }

    private static T Lookup<T>(IReadOnlyDictionary<string, T> dict, string id, string what) where T : class
    {
        if (dict != null && dict.TryGetValue(id, out var value)) return value;
        Log.Warn($"[AdventurerDB] Не найден {what}: {id}");
        return null;
    }
}