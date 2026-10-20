namespace AdventurersGuild.Domain.Units;

using System.Collections.Generic;
using AdventurersGuild.Core;
using AdventurersGuild.Domain.Names;

public static class UnitDatabase
{
    public static IReadOnlyDictionary<string, ClassInfo> Classes { get; private set; }
    public static IReadOnlyDictionary<string, SpeciesInfo> Species { get; private set; }
    public static IReadOnlyDictionary<string, StatInfo> Stats { get; private set; }
    public static IReadOnlyDictionary<string, SkillInfo> Skills { get; private set; }
    public static IReadOnlyDictionary<string, StatusInfo> Statuses { get; private set; }
    public static NameDatabase Names { get; private set; }

    private static Dictionary<string, string> skillToStat = [];

    public static int SkillCount => skillToStat?.Count ?? 0;

    // === Install ===

    public static void Install(
        IReadOnlyDictionary<string, ClassInfo> classes = null,
        IReadOnlyDictionary<string, SpeciesInfo> species = null,
        IReadOnlyDictionary<string, StatInfo> stats = null,
        IReadOnlyDictionary<string, SkillInfo> skills = null,
        IReadOnlyDictionary<string, StatusInfo> statuses = null,
        NameDatabase names = null)
    {
        if (classes != null) Classes = classes;
        if (species != null) Species = species;
        if (stats != null) Stats = stats;
        if (skills != null) Skills = skills;
        if (statuses != null) Statuses = statuses;
        if (names != null) Names = names;

        BuildSkillIndex();
    }

    private static void BuildSkillIndex()
    {
        skillToStat = [];

        if (Skills == null) return;

        foreach (var kv in Skills)
        {
            var skill = kv.Value;
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

    public static ClassInfo GetClass(string id) => Lookup(Classes, id, "класс");
    public static SpeciesInfo GetSpecies(string id) => Lookup(Species, id, "вид");
    public static StatInfo GetStat(string id) => Lookup(Stats, id, "стат");
    public static StatusInfo GetStatus(string id) => Lookup(Statuses, id, "статус");

    public static string StatForSkill(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return null;
        return skillToStat != null && skillToStat.TryGetValue(skillId, out var s) ? s : null;
    }

    public static SkillInfo GetSkill(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return null;
        return Skills != null && Skills.TryGetValue(skillId, out var s) ? s : null;
    }

    public static bool IsValidSkill(string skillId)
        => !string.IsNullOrEmpty(skillId)
           && skillToStat != null
           && skillToStat.ContainsKey(skillId);

    public static string ClassName(string id)
    {
        ClassInfo c = GetClass(id);
        return c?.Name.Get(Loc.Language) ?? id;
    }

    public static string SpeciesName(string id)
    {
        SpeciesInfo s = GetSpecies(id);

        if (s == null) return id;

        var name = s.Name?.Get(Loc.Language);
        return string.IsNullOrEmpty(name) ? id : name;
    }

    public static string StatName(string id)
    {
        StatInfo s = GetStat(id);
        return s?.Name.Get(Loc.Language) ?? id;
    }

    public static string StatDescription(string id)
    {
        StatInfo s = GetStat(id);
        return s?.Description.Get(Loc.Language) ?? "";
    }

    public static string StatusName(string id)
    {
        StatusInfo s = GetStatus(id);
        return s?.Name.Get(Loc.Language) ?? id;
    }

    public static bool IsFree(UnitModel a)
    {
        if (a == null) return false;
        StatusInfo info = GetStatus(a.StatusId);
        return info == null || !info.BlocksAssignment;
    }

    private static T Lookup<T>(IReadOnlyDictionary<string, T> dict, string id, string what) where T : class
    {
        if (dict != null && dict.TryGetValue(id, out var value)) return value;
        Log.Warn($"[AdventurerDB] Не найден {what}: {id}");
        return null;
    }
}