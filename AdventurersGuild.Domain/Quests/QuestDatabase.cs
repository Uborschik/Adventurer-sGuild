namespace AdventurersGuild.Domain.Quests;

using System.Collections.Generic;
using AdventurersGuild.Core;

public static class QuestDatabase
{
    public static IReadOnlyList<QuestGradeInfo> Grades { get; private set; }
    public static IReadOnlyDictionary<QuestGradeRole, QuestGradeInfo> GradesByRole { get; private set; }
    public static IReadOnlyDictionary<string, Blueprint> Blueprints { get; private set; }
    public static IReadOnlyDictionary<string, CreatureInfo> Creatures { get; private set; }
    public static IReadOnlyDictionary<string, LocationInfo> Locations { get; private set; }
    public static IReadOnlyDictionary<string, QuestPhaseTemplate> Phases { get; private set; }
    public static IReadOnlyDictionary<string, TagInfo> Tags { get; private set; }

    // === Install ===

    public static void Install(
        IReadOnlyDictionary<string, TagInfo> tags,
        IReadOnlyList<QuestGradeInfo> grades,
        IReadOnlyDictionary<string, QuestPhaseTemplate> phases,
        IReadOnlyDictionary<string, CreatureInfo> creatures,
        IReadOnlyDictionary<string, LocationInfo> locations,
        IReadOnlyDictionary<string, Blueprint> blueprints)
    {
        Tags = tags;
        Phases = phases;
        Creatures = creatures;
        Locations = locations;
        Blueprints = blueprints;

        Grades = grades;
        GradesByRole = BuildGradesByRole(grades);
    }

    private static IReadOnlyDictionary<QuestGradeRole, QuestGradeInfo> BuildGradesByRole(
        IReadOnlyList<QuestGradeInfo> grades)
    {
        var byRole = new Dictionary<QuestGradeRole, QuestGradeInfo>();
        if (grades == null) return byRole;

        foreach (var g in grades)
        {
            if (!System.Enum.TryParse<QuestGradeRole>(g.Role, ignoreCase: true, out var role)) continue;
            if (byRole.ContainsKey(role)) continue;
            byRole[role] = g;
        }

        return byRole;
    }

    // === Lookup ===

    public static Blueprint GetBlueprint(string id) => Lookup(Blueprints, id, "blueprint");

    public static string BlueprintName(string id)
    {
        var b = GetBlueprint(id);
        return b?.Name.Get(Loc.Language) ?? id;
    }

    public static QuestGradeInfo GetByRole(QuestGradeRole role)
    {
        if (GradesByRole != null && GradesByRole.TryGetValue(role, out var g)) return g;
        Log.Error($"[QuestDB] Нет градации с ролью {role}");
        return null;
    }

    public static string GradeName(QuestGradeRole role)
    {
        var g = GetByRole(role);
        return g?.Name.Get(Loc.Language) ?? role.ToString();
    }

    public static CreatureInfo GetCreature(string id) => Lookup(Creatures, id, "существо");

    public static LocationInfo GetLocation(string id) => Lookup(Locations, id, "локация");

    public static QuestPhaseTemplate GetPhase(string id) => Lookup(Phases, id, "фаза");

    public static TagInfo GetTag(string id) => Lookup(Tags, id, "тег");

    private static T Lookup<T>(IReadOnlyDictionary<string, T> dict, string id, string what) where T : class
    {
        if (dict != null && dict.TryGetValue(id, out var value)) return value;
        Log.Warn($"[QuestDB] Не найден {what}: {id}");
        return null;
    }
}