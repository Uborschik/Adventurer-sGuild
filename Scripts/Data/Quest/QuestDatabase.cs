using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AdventurersGuild.Core;
using AdventurersGuild.Domain.Quests;

namespace AdventurersGuild.Data.Quest;

public enum QuestGradeRole
{
    Disaster,
    Failure,
    Success,
    Triumph
}

public static partial class QuestDatabase
{
    public static IReadOnlyDictionary<string, QuestTypeInfo> Types { get; private set; }
    public static IReadOnlyList<QuestGradeInfo> Grades { get; private set; }
    public static IReadOnlyDictionary<QuestGradeRole, QuestGradeInfo> GradesByRole { get; private set; }
    public static IReadOnlyList<QuestTemplate> Templates { get; private set; }
    public static IReadOnlyDictionary<string, CreatureInfo> Creatures { get; private set; }
    public static IReadOnlyDictionary<string, LocationInfo> Locations { get; private set; }
    public static IReadOnlyDictionary<string, QuestPhaseTemplate> Phases { get; private set; }

    private static QuestPhaseTemplate ClonePhase(QuestPhaseTemplate original)
    {
        return new QuestPhaseTemplate
        {
            Id = original.Id,
            Name = original.Name,
            BaseDurationMinutes = original.BaseDurationMinutes,
            Critical = original.Critical,
            ExpReward = original.ExpReward,
            Solutions = original.Solutions
                .Select(s => new PhaseSolution
                {
                    Skills = new Dictionary<string, double>(s.Skills)
                })
                .ToList()
        };
    }

    private static void InsertPhase(List<QuestPhaseTemplate> phases, QuestPhaseTemplate newPhase, string before)
    {
        if (!string.IsNullOrEmpty(before))
        {
            int idx = phases.FindIndex(p => p.Id == before);
            if (idx >= 0)
            {
                phases.Insert(idx, newPhase);
                return;
            }

            Log.Warn($"[QuestDB] Фаза '{newPhase.Id}': 'before' = '{before}' не найдена, дефолт");
        }

        int criticalIdx = phases.FindIndex(p => p.Critical);
        if (criticalIdx >= 0)
        {
            phases.Insert(criticalIdx, newPhase);
            return;
        }

        phases.Add(newPhase);
    }

    public static QuestTypeInfo GetType(string id) => Lookup(Types, id, "тип квеста");

    public static string TypeName(string id)
    {
        var t = GetType(id);
        return t?.Name.Get(Loc.Language) ?? id;
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

    public static CreatureInfo GetCreature(string id)
    {
        if (Creatures != null && Creatures.TryGetValue(id, out var c)) return c;
        Log.Warn($"[QuestDB] Не найдено существо: {id}");
        return null;
    }

    public static LocationInfo GetLocation(string id)
    {
        if (Locations != null && Locations.TryGetValue(id, out var l)) return l;
        Log.Warn($"[QuestDB] Не найдена локация: {id}");
        return null;
    }

    private static T Lookup<T>(IReadOnlyDictionary<string, T> dict, string id, string what) where T : class
    {
        if (dict != null && dict.TryGetValue(id, out var value)) return value;
        Log.Warn($"[QuestDB] Не найден {what}: {id}");
        return null;
    }
}