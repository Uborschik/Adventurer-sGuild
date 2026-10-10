using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Core;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Names;

namespace AdventurersGuild.Data.Adventurer;

public static partial class AdventurerDatabase
{
    public static IReadOnlyDictionary<string, AdventurerClassInfo> Classes { get; private set; }
    public static IReadOnlyDictionary<string, AdventurerRaceInfo> Races { get; private set; }
    public static IReadOnlyDictionary<string, AdventurerStatInfo> Stats { get; private set; }
    public static IReadOnlyDictionary<string, AdventurerStatusInfo> Statuses { get; private set; }
    public static IReadOnlyList<AdventurerTemplateInfo> Templates { get; private set; }
    public static NameDatabase Names { get; private set; }

    private static Dictionary<string, string> skillToStat;
    private static Dictionary<string, SkillInfo> skillsById;

    public static AdventurerClassInfo GetClass(string id) => Lookup(Classes, id, "класс");
    public static AdventurerRaceInfo GetRace(string id) => Lookup(Races, id, "раса");
    public static AdventurerStatInfo GetStat(string id) => Lookup(Stats, id, "стат");
    public static AdventurerStatusInfo GetStatus(string id) => Lookup(Statuses, id, "статус");

    public static string StatForSkill(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return null;
        return skillToStat != null && skillToStat.TryGetValue(skillId, out var s)
            ? s
            : null;
    }

    public static SkillInfo GetSkill(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return null;
        return skillsById != null && skillsById.TryGetValue(skillId, out var s) ? s : null;
    }

    public static bool IsValidSkill(string skillId) => !string.IsNullOrEmpty(skillId)
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