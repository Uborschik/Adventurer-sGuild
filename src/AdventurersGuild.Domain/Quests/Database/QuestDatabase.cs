namespace AdventurersGuild.Domain.Quests;

using System.Collections.Generic;
using AdventurersGuild.Core;

public static class QuestDatabase
{
    public static IReadOnlyDictionary<string, RoleInfo> Roles { get; private set; }
    public static IReadOnlyDictionary<string, InstructionInfo> Instructions { get; private set; }
    public static IReadOnlyDictionary<string, PhaseInfo> Phases { get; private set; }
    public static IReadOnlyDictionary<string, BestiaryInfo> Habitats { get; private set; }
    public static IReadOnlyDictionary<string, QuestTemplateInfo> Templates { get; private set; }
    public static IReadOnlyDictionary<string, LocationInfo> Locations { get; private set; }
    public static IReadOnlyDictionary<string, QuestGradeInfo> Grades { get; private set; }

    public static void Install(
        IReadOnlyDictionary<string, RoleInfo> roles,
        IReadOnlyDictionary<string, InstructionInfo> instructions,
        IReadOnlyDictionary<string, PhaseInfo> phases,
        IReadOnlyDictionary<string, BestiaryInfo> habitats,
        IReadOnlyDictionary<string, QuestTemplateInfo> templates,
        IReadOnlyDictionary<string, LocationInfo> locations,
        IReadOnlyDictionary<string, QuestGradeInfo> grades)
    {
        Roles = roles;
        Instructions = instructions;
        Phases = phases;
        Habitats = habitats;
        Templates = templates;
        Locations = locations;
        Grades = grades;
    }

    public static LocationInfo GetLocation(string id)
        => Locations != null && Locations.TryGetValue(id, out LocationInfo l) ? l : null;

    public static string TemplateName(string id)
        => Templates != null && Templates.TryGetValue(id, out QuestTemplateInfo t) ? t.Name.Get(Loc.Language) : id;

    public static string GradeName(QuestGradeRole role)
    {
        if (Grades == null) return role.ToString();

        var key = role.ToString();

        if (Grades.TryGetValue(key, out QuestGradeInfo g))
            return g.Name.Get(Loc.Language);

        return role.ToString();
    }
}