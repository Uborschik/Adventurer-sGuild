namespace AdventurersGuild.Data.Quest;

using System;
using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Core;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Domain.Quests;

public static partial class QuestDatabase
{
    private const string DataPath = "res://Resources/Data/Quest/";

    public static void Load()
    {
        LoadTags();
        LoadGrades();
        LoadPhases();
        LoadCreatures();
        LoadLocations();
        LoadBlueprints();

        Validate();
    }

    private static void LoadTags()
    {
        Tags = JsonLoader.Load<TagDatabase>(DataPath + "Tags.json")
            .Tags.ToDictionary(t => t.Id);
    }

    private static void LoadGrades()
    {
        var gradeList = JsonLoader.Load<QuestGradeDatabase>(DataPath + "QuestGrades.json").Grades;
        Grades = gradeList;

        var byRole = new Dictionary<QuestGradeRole, QuestGradeInfo>();
        foreach (var grade in gradeList)
        {
            if (!Enum.TryParse<QuestGradeRole>(grade.Role, ignoreCase: true, out var role))
            {
                Log.Error($"[QuestDB] Неизвестная роль градации: '{grade.Role}'");
                continue;
            }
            if (byRole.ContainsKey(role))
            {
                Log.Error($"[QuestDB] Роль '{role}' дублируется");
                continue;
            }
            byRole[role] = grade;
        }

        foreach (QuestGradeRole role in Enum.GetValues<QuestGradeRole>())
            if (!byRole.ContainsKey(role))
                Log.Error($"[QuestDB] Нет градации с ролью {role}");

        GradesByRole = byRole;
    }

    private static void LoadPhases()
    {
        var db = JsonLoader.Load<QuestPhaseDatabase>(DataPath + "QuestPhases.json");
        Phases = db.Phases.ToDictionary(p => p.Id);
    }

    private static void LoadCreatures()
    {
        var list = JsonLoader.Load<CreatureDatabase>(DataPath + "Creatures.json").Creatures;
        Creatures = list.ToDictionary(c => c.Id);
    }

    private static void LoadLocations()
    {
        var list = JsonLoader.Load<LocationDatabase>(DataPath + "Locations.json").Locations;
        Locations = list.ToDictionary(l => l.Id);
    }

    private static void LoadBlueprints()
    {
        var list = JsonLoader.Load<BlueprintDatabase>(DataPath + "Blueprints.json").Blueprints;
        var dict = list.ToDictionary(b => b.Id);

        foreach (var bp in dict.Values)
            BuildOutgoing(bp);

        Blueprints = dict;
    }

    private static void BuildOutgoing(Blueprint bp)
    {
        var byId = bp.Roles.ToDictionary(r => r.Id);

        foreach (var role in bp.Roles)
            role.Outgoing.Clear();

        foreach (var role in bp.Roles)
        {
            foreach (var edge in role.Incoming)
            {
                if (byId.TryGetValue(edge.From, out var source))
                {
                    source.Outgoing.Add(new BlueprintOutgoing(role.Id, edge.Condition, edge.DcDelta, edge.DurationDeltaMinutes));
                }
            }
        }
    }

    public static void ApplyData(
        IReadOnlyDictionary<string, Blueprint> blueprints = null,
        IReadOnlyDictionary<string, CreatureInfo> creatures = null,
        IReadOnlyDictionary<string, LocationInfo> locations = null,
        IReadOnlyDictionary<string, QuestPhaseTemplate> phases = null,
        IReadOnlyList<QuestGradeInfo> grades = null,
        IReadOnlyDictionary<string, TagInfo> tags = null)
    {
        if (blueprints != null) Blueprints = blueprints;
        if (creatures != null) Creatures = creatures;
        if (locations != null) Locations = locations;
        if (phases != null) Phases = phases;
        if (tags != null) Tags = tags;

        if (grades != null)
        {
            Grades = grades;
            var byRole = new Dictionary<QuestGradeRole, QuestGradeInfo>();
            foreach (var g in grades)
            {
                if (!Enum.TryParse<QuestGradeRole>(g.Role, ignoreCase: true, out var role)) continue;
                if (byRole.ContainsKey(role)) continue;
                byRole[role] = g;
            }
            GradesByRole = byRole;
        }
    }
}