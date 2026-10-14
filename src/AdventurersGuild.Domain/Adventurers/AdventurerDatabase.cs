namespace AdventurersGuild.Domain.Adventurers;

using System.Collections.Generic;
using AdventurersGuild.Core;
using AdventurersGuild.Domain.Names;

public static class AdventurerDatabase
{
    public static IReadOnlyDictionary<string, AdventurerClassInfo> Classes { get; private set; }
    public static IReadOnlyDictionary<string, AdventurerRaceInfo> Races { get; private set; }
    public static IReadOnlyDictionary<string, AdventurerStatInfo> Stats { get; private set; }
    public static IReadOnlyDictionary<string, SkillInfo> Skills { get; private set; }
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
        IReadOnlyDictionary<string, SkillInfo> skills = null,
        IReadOnlyDictionary<string, AdventurerStatusInfo> statuses = null,
        IReadOnlyList<AdventurerTemplateInfo> templates = null,
        NameDatabase names = null)
    {
        if (classes != null) Classes = classes;
        if (races != null) Races = races;
        if (stats != null) Stats = stats;
        if (skills != null) Skills = skills;
        if (statuses != null) Statuses = statuses;
        if (templates != null) Templates = templates;
        if (names != null) Names = names;

        BuildSkillIndex();
    }

    private static void BuildSkillIndex()
    {
        skillToStat = [];

        if (skillsById == null) return;

        foreach (KeyValuePair<string, SkillInfo> kv in skillsById)
        {
            SkillInfo skill = kv.Value;
            if (skill == null || string.IsNullOrEmpty(skill.Id)) continue;

            if (string.IsNullOrEmpty(skill.StatId))
            {
                Log.Warn($"[AdventurerDB] Навык '{skill.Id}': не задан statId");
                continue;
            }

            if (skillToStat.ContainsKey(skill.Id))
            {
                Log.Warn($"[AdventurerDB] Навык '{skill.Id}' дублируется");
                continue;
            }

            skillToStat[skill.Id] = skill.StatId;
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
        AdventurerClassInfo c = GetClass(id);
        return c?.Name.Get(Loc.Language) ?? id;
    }

    public static string RaceName(string id)
    {
        AdventurerRaceInfo r = GetRace(id);
        return r?.Name.Get(Loc.Language) ?? id;
    }

    public static string StatName(string id)
    {
        AdventurerStatInfo s = GetStat(id);
        return s?.Name.Get(Loc.Language) ?? id;
    }

    public static string StatDescription(string id)
    {
        AdventurerStatInfo s = GetStat(id);
        return s?.Description.Get(Loc.Language) ?? "";
    }

    public static string StatusName(string id)
    {
        AdventurerStatusInfo s = GetStatus(id);
        return s?.Name.Get(Loc.Language) ?? id;
    }

    public static bool IsFree(AdventurerModel a)
    {
        if (a == null) return false;
        AdventurerStatusInfo info = GetStatus(a.StatusId);
        return info == null || !info.BlocksAssignment;
    }

    private static T Lookup<T>(IReadOnlyDictionary<string, T> dict, string id, string what) where T : class
    {
        if (dict != null && dict.TryGetValue(id, out var value)) return value;
        Log.Warn($"[AdventurerDB] Не найден {what}: {id}");
        return null;
    }
}