using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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

    private static Dictionary<(string templateId, string creatureId), ResolvedTemplate> resolvedCombos;

    public static IReadOnlyDictionary<(string templateId, string creatureId), ResolvedTemplate> AllResolved => resolvedCombos;

    private static QuestTier ParseTier(string tier)
    {
        if (string.IsNullOrEmpty(tier)) return QuestTier.Normal;
        if (Enum.TryParse<QuestTier>(tier, ignoreCase: true, out var parsed))
            return parsed;
        Log.Error($"[QuestDB] Неизвестный tier '{tier}', использую Normal");
        return QuestTier.Normal;
    }

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

    private static void ApplyModifier(QuestPhaseTemplate phase, PhaseModifier mod)
    {
        if (mod == null) return;

        if (mod.DurationDeltaMinutes.HasValue)
            phase.BaseDurationMinutes += mod.DurationDeltaMinutes.Value;

        foreach (var sol in phase.Solutions)
        {
            var keys = new List<string>(sol.Skills.Keys);
            foreach (var skillId in keys)
            {
                if (mod.DcDelta.HasValue)
                    sol.Skills[skillId] += mod.DcDelta.Value;

                if (mod.StatModifiers != null)
                {
                    string statId = AdventurerDatabase.StatForSkill(skillId);
                    if (statId != null
                        && mod.StatModifiers.TryGetValue(statId, out var statMod))
                    {
                        sol.Skills[skillId] += statMod.DcDelta;
                    }
                }
            }
        }
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

    private static ResolvedTemplate ResolveTemplate(QuestTemplate t, string creatureId)
    {
        var phases = new List<QuestPhaseTemplate>();

        if (t.Phases != null)
        {
            foreach (var phaseId in t.Phases)
            {
                if (!Phases.TryGetValue(phaseId, out var original))
                {
                    Log.Error($"[QuestDB] Шаблон '{t.Id}': неизвестная фаза '{phaseId}'");
                    continue;
                }
                phases.Add(ClonePhase(original));
            }
        }

        CreatureInfo creature = null;
        if (!string.IsNullOrEmpty(creatureId))
            Creatures.TryGetValue(creatureId, out creature);

        if (creature?.PhaseEffects != null)
        {
            // Модификаторы
            if (creature.PhaseEffects.Modifies != null)
            {
                foreach (var kv in creature.PhaseEffects.Modifies)
                {
                    var phase = phases.FirstOrDefault(p => p.Id == kv.Key);
                    if (phase == null)
                    {
                        Log.Warn($"[QuestDB] '{t.Id}'+'{creatureId}': модификатор для отсутствующей фазы '{kv.Key}'");
                        continue;
                    }
                    ApplyModifier(phase, kv.Value);
                }
            }

            // Добавленные фазы
            if (creature.PhaseEffects.Adds != null)
            {
                foreach (var added in creature.PhaseEffects.Adds)
                {
                    if (phases.Any(p => p.Id == added.Phase)) continue;

                    Phases.TryGetValue(added.Phase, out var orig);

                    var newPhase = new QuestPhaseTemplate
                    {
                        Id = added.Phase,
                        Name = orig.Name,
                        BaseDurationMinutes = orig?.BaseDurationMinutes ?? 240,
                        Critical = orig?.Critical ?? false,
                        ExpReward = orig?.ExpReward ?? 20,
                        Solutions = added.Solutions
                            .Select(s => new PhaseSolution
                            {
                                Skills = new Dictionary<string, double>(s.Skills)
                            })
                            .ToList()
                    };

                    InsertPhase(phases, newPhase, added.Before);
                }
            }
        }

        return new ResolvedTemplate
        {
            Phases = phases,
            Tier = ParseTier(t.Tier),
        };
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

    public static ResolvedTemplate GetResolved(string templateId, string creatureId)
    {
        creatureId ??= "";
        if (resolvedCombos != null && resolvedCombos.TryGetValue((templateId, creatureId), out var r))
            return r;
        Log.Error($"[QuestDB] Нет resolved: template='{templateId}', creature='{creatureId}'");
        return null;
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

public class ResolvedTemplate
{
    public List<QuestPhaseTemplate> Phases { get; set; }
    public QuestTier Tier { get; set; }
}