namespace AdventurersGuild.Data.Quest;

using System;
using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Core;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Domain.Quests;

public static class QuestLoader
{
    private const string dataPath = "Data/Quest/";

    public static void Load()
    {
        var tags = LoadTags();
        var grades = LoadGrades();
        var phases = LoadPhases();
        var creatures = LoadCreatures();
        var locations = LoadLocations();
        var blueprints = LoadBlueprints();

        QuestDatabase.Install(tags, grades, phases, creatures, locations, blueprints);

        QuestValidator.Validate();
    }

    private static Dictionary<string, TagInfo> LoadTags()
    {
        return JsonLoader.Load<TagDatabase>(dataPath + "Tags.json")
            .Tags.ToDictionary(t => t.Id);
    }

    private static List<QuestGradeInfo> LoadGrades()
    {
        var gradeList = JsonLoader.Load<QuestGradeDatabase>(dataPath + "QuestGrades.json").Grades;

        foreach (var grade in gradeList)
        {
            if (!Enum.TryParse<QuestGradeRole>(grade.Role, ignoreCase: true, out _))
                Log.Error($"[QuestDB] Неизвестная роль градации: '{grade.Role}'");
        }

        return gradeList;
    }

    private static Dictionary<string, QuestPhaseTemplate> LoadPhases()
    {
        var db = JsonLoader.Load<QuestPhaseDatabase>(dataPath + "QuestPhases.json");
        return db.Phases.ToDictionary(p => p.Id);
    }

    private static Dictionary<string, CreatureInfo> LoadCreatures() => [];

    private static Dictionary<string, LocationInfo> LoadLocations()
    {
        var list = JsonLoader.Load<LocationDatabase>(dataPath + "Locations.json").Locations;
        return list.ToDictionary(l => l.Id);
    }

    private static Dictionary<string, Blueprint> LoadBlueprints()
    {
        var list = JsonLoader.Load<BlueprintDatabase>(dataPath + "Blueprints.json").Blueprints;
        var dict = list.ToDictionary(b => b.Id);

        foreach (var bp in dict.Values)
            BuildOutgoing(bp);

        return dict;
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
                    source.Outgoing.Add(new BlueprintOutgoing(role.Id, edge.Condition, edge.DcDelta, edge.DurationDeltaMinutes));
            }
        }
    }
}