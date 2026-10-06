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

public static class QuestDatabase
{
    private const string DataPath = "res://Resources/Data/Quest/";

    public static IReadOnlyDictionary<string, QuestTypeInfo> Types { get; private set; }
    public static IReadOnlyList<QuestGradeInfo> Grades { get; private set; }
    public static IReadOnlyDictionary<QuestGradeRole, QuestGradeInfo> GradesByRole { get; private set; }
    public static IReadOnlyList<QuestTemplate> Templates { get; private set; }
    public static IReadOnlyDictionary<string, CreatureInfo> Creatures { get; private set; }
    public static IReadOnlyDictionary<string, LocationInfo> Locations { get; private set; }
    public static IReadOnlyDictionary<string, QuestPhaseTemplate> Phases { get; private set; }

    private static Dictionary<(string templateId, string creatureId), ResolvedTemplate> resolvedCombos;

    public static IReadOnlyDictionary<(string templateId, string creatureId), ResolvedTemplate> AllResolved
        => resolvedCombos;

    public static void Load()
    {
        Types = JsonLoader.Load<QuestTypeDatabase>(DataPath + "QuestTypes.json")
            .Types.ToDictionary(t => t.Id);

        LoadGrades();
        LoadPhases();
        LoadCreatures();
        LoadLocations();
        LoadTemplates();

        Validate();
        ValidateDeclensions();
    }

    // === Загрузка ===

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

    private static void LoadTemplates()
    {
        var templateList = JsonLoader.Load<QuestTemplateDatabase>(DataPath + "QuestTemplates.json").Templates;
        Templates = templateList;

        resolvedCombos = new Dictionary<(string, string), ResolvedTemplate>();

        foreach (var t in templateList)
        {
            if (t.Creatures == null || t.Creatures.Count == 0)
            {
                var resolved = ResolveTemplate(t, null);
                if (resolved != null)
                    resolvedCombos[(t.Id, "")] = resolved;
                continue;
            }

            foreach (var creatureId in t.Creatures)
            {
                var resolved = ResolveTemplate(t, creatureId);
                if (resolved != null)
                    resolvedCombos[(t.Id, creatureId)] = resolved;
            }
        }
    }

    // === Резолв ===

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

    private static void InsertPhase(List<QuestPhaseTemplate> phases,
                                     QuestPhaseTemplate newPhase,
                                     string before)
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

    // === Валидация ===

    private static void Validate()
    {
        int errors = 0;

        foreach (var t in Types.Values)
        {
            if (string.IsNullOrEmpty(t.Name.Ru) || string.IsNullOrEmpty(t.Name.En))
            {
                Log.Error($"[QuestDB] Тип '{t.Id}': неполное name");
                errors++;
            }
        }

        foreach (var g in Grades)
        {
            if (string.IsNullOrEmpty(g.Name.Ru) || string.IsNullOrEmpty(g.Name.En))
            {
                Log.Error($"[QuestDB] Градация '{g.Role}': неполное name");
                errors++;
            }
        }

        foreach (var p in Phases.Values)
        {
            if (string.IsNullOrEmpty(p.Id))
            {
                Log.Error("[QuestDB] Фаза без id");
                errors++;
                continue;
            }
            if (p.ExpReward < 0)
            {
                Log.Error($"[QuestDB] Фаза '{p.Id}': expReward < 0");
                errors++;
            }
            if (p.Solutions == null || p.Solutions.Count == 0)
            {
                if (p.Id != "short_rest")
                {
                    Log.Error($"[QuestDB] Фаза '{p.Id}': пустой solutions");
                    errors++;
                }
                continue;
            }
            foreach (var sol in p.Solutions)
            {
                if (sol.Skills == null || sol.Skills.Count == 0)
                {
                    Log.Error($"[QuestDB] Фаза '{p.Id}': пустое решение");
                    errors++;
                    continue;
                }
                foreach (var kv in sol.Skills)
                {
                    if (!AdventurerDatabase.IsValidSkill(kv.Key))
                    {
                        Log.Error($"[QuestDB] Фаза '{p.Id}': неизвестный навык '{kv.Key}'");
                        errors++;
                    }
                    if (kv.Value <= 0)
                    {
                        Log.Error($"[QuestDB] Фаза '{p.Id}': DC <= 0 у '{kv.Key}'");
                        errors++;
                    }
                }
            }
        }

        foreach (var t in Templates)
        {
            if (string.IsNullOrEmpty(t.Name.Ru) || string.IsNullOrEmpty(t.Name.En))
            {
                Log.Error($"[QuestDB] Шаблон '{t.Id}': неполное name");
                errors++;
            }
            if (t.LevelRange == null)
            {
                Log.Error($"[QuestDB] Шаблон '{t.Id}': нет levelRange");
                errors++;
            }
            if (string.IsNullOrEmpty(t.Description.Ru) || string.IsNullOrEmpty(t.Description.En))
            {
                Log.Error($"[QuestDB] Шаблон '{t.Id}': неполное description");
                errors++;
            }
            if (string.IsNullOrEmpty(t.Type) || !Types.ContainsKey(t.Type))
            {
                Log.Error($"[QuestDB] Шаблон '{t.Id}': тип '{t.Type}' не найден");
                errors++;
            }
            if (t.Phases == null || t.Phases.Count == 0)
            {
                Log.Error($"[QuestDB] Шаблон '{t.Id}': пустой phases");
                errors++;
            }
            else
            {
                foreach (var pid in t.Phases)
                {
                    if (!Phases.ContainsKey(pid))
                    {
                        Log.Error($"[QuestDB] Шаблон '{t.Id}': фаза '{pid}' не найдена");
                        errors++;
                    }
                }
            }
            if (t.Creatures != null)
                foreach (var c in t.Creatures)
                    if (string.IsNullOrEmpty(c) || !Creatures.ContainsKey(c))
                    {
                        Log.Error($"[QuestDB] Шаблон '{t.Id}': creature '{c}' не найден");
                        errors++;
                    }
            if (t.Locations != null)
                foreach (var l in t.Locations)
                    if (string.IsNullOrEmpty(l) || !Locations.ContainsKey(l))
                    {
                        Log.Error($"[QuestDB] Шаблон '{t.Id}': location '{l}' не найден");
                        errors++;
                    }
        }

        foreach (var c in Creatures.Values)
        {
            if (string.IsNullOrEmpty(c.Name?.Ru?.Nom))
            {
                Log.Error($"[QuestDB] Creature '{c.Id}': нет русского имени");
                errors++;
            }
            if (string.IsNullOrEmpty(c.Name?.En))
            {
                Log.Error($"[QuestDB] Creature '{c.Id}': нет английского имени");
                errors++;
            }
        }

        foreach (var l in Locations.Values)
        {
            if (string.IsNullOrEmpty(l.Name?.Ru?.Nom))
            {
                Log.Error($"[QuestDB] Location '{l.Id}': нет русского имени");
                errors++;
            }
            if (string.IsNullOrEmpty(l.Name?.En))
            {
                Log.Error($"[QuestDB] Location '{l.Id}': нет английского имени");
                errors++;
            }
        }

        if (errors == 0)
            Log.Info($"[QuestDB] Валидация: ok ({Types.Count} типов, {Grades.Count} градаций, " +
                     $"{Phases.Count} фаз, {Templates.Count} шаблонов, " +
                     $"{Creatures.Count} существ, {Locations.Count} локаций)");
        else
            Log.Error($"[QuestDB] Валидация: {errors} ошибок");
    }

    private static readonly HashSet<string> ValidCases = new() { "nom", "gen", "dat", "acc", "ins", "pre" };

    private static void ValidateDeclensions()
    {
        var neededCases = new HashSet<string>();
        var pattern = new Regex(@"\{(\w+):(\w+)\}");

        foreach (var t in Templates)
        {
            string ru = t.Description.Ru;
            if (string.IsNullOrEmpty(ru)) continue;

            foreach (Match m in pattern.Matches(ru))
            {
                string caseCode = m.Groups[2].Value;
                if (!ValidCases.Contains(caseCode))
                {
                    Log.Error($"[QuestDB] Шаблон '{t.Id}': неизвестный падеж '{caseCode}'");
                    continue;
                }
                neededCases.Add(caseCode);
            }
        }

        if (neededCases.Count == 0) return;

        int errors = 0;
        foreach (var c in Creatures.Values)
            foreach (var caseCode in neededCases)
                if (string.IsNullOrEmpty(c.Name?.Ru?.Get(caseCode)))
                {
                    Log.Error($"[QuestDB] Creature '{c.Id}': нет формы '{caseCode}'");
                    errors++;
                }
        foreach (var l in Locations.Values)
            foreach (var caseCode in neededCases)
                if (string.IsNullOrEmpty(l.Name?.Ru?.Get(caseCode)))
                {
                    Log.Error($"[QuestDB] Location '{l.Id}': нет формы '{caseCode}'");
                    errors++;
                }

        if (errors == 0)
            Log.Info($"[QuestDB] Валидация падежей: ok");
    }

    // === Публичное API ===

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