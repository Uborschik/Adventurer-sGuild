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
    public static IReadOnlyDictionary<string, QuestOutcomeInfo> Outcomes { get; private set; }

    private static Dictionary<string, QuestTagInfo> tagsById;
    private static Dictionary<(string templateId, string creatureId), ResolvedTemplate> resolvedCombos;

    public static IReadOnlyDictionary<(string templateId, string creatureId), ResolvedTemplate> AllResolved
    => resolvedCombos;

    public static IReadOnlyDictionary<string, QuestTagInfo> AllTags
        => tagsById;

    public static void Load()
    {
        Types = JsonLoader.Load<QuestTypeDatabase>(DataPath + "QuestTypes.json")
            .Types.ToDictionary(t => t.Id);

        LoadGrades();
        LoadTags();
        LoadCreatures();
        LoadLocations();
        LoadTemplates();
        LoadOutcomes();

        Validate();
        ValidateDeclensions();
    }

    private static void Validate()
    {
        int errors = 0;

        // === Типы ===
        foreach (var t in Types.Values)
        {
            if (string.IsNullOrEmpty(t.Name.Ru) || string.IsNullOrEmpty(t.Name.En))
            {
                Log.Error($"[QuestDB] Тип '{t.Id}': неполное name");
                errors++;
            }
        }

        // === Градации ===
        foreach (var g in Grades)
        {
            if (string.IsNullOrEmpty(g.Name.Ru) || string.IsNullOrEmpty(g.Name.En))
            {
                Log.Error($"[QuestDB] Градация '{g.Role}': неполное name");
                errors++;
            }
        }

        // === Шаблоны ===
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
            else
            {
                if (t.LevelRange.Min < 1)
                {
                    Log.Error($"[QuestDB] Шаблон '{t.Id}': levelRange.min < 1");
                    errors++;
                }
                if (t.LevelRange.Max < t.LevelRange.Min)
                {
                    Log.Error($"[QuestDB] Шаблон '{t.Id}': levelRange.max < min");
                    errors++;
                }
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

            if (t.Creatures != null)
            {
                foreach (var c in t.Creatures)
                {
                    if (string.IsNullOrEmpty(c) || !Creatures.ContainsKey(c))
                    {
                        Log.Error($"[QuestDB] Шаблон '{t.Id}': creature '{c}' не найден");
                        errors++;
                    }
                }
            }

            if (t.Locations != null)
            {
                foreach (var l in t.Locations)
                {
                    if (string.IsNullOrEmpty(l) || !Locations.ContainsKey(l))
                    {
                        Log.Error($"[QuestDB] Шаблон '{t.Id}': location '{l}' не найден");
                        errors++;
                    }
                }
            }

            if (t.Tags != null)
            {
                foreach (var tag in t.Tags)
                {
                    if (string.IsNullOrEmpty(tag) || !tagsById.ContainsKey(tag))
                    {
                        Log.Error($"[QuestDB] Шаблон '{t.Id}': тег '{tag}' не найден");
                        errors++;
                    }
                }
            }
        }

        // === Существа ===
        foreach (var c in Creatures.Values)
        {
            if (c.Tags != null)
            {
                foreach (var tag in c.Tags)
                {
                    if (string.IsNullOrEmpty(tag) || !tagsById.ContainsKey(tag))
                    {
                        Log.Error($"[QuestDB] Creature '{c.Id}': тег '{tag}' не найден");
                        errors++;
                    }
                }
            }

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

        // === Локации ===
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

        // === Outcomes ===
        if (Outcomes == null)
        {
            Log.Error("[QuestDB] Outcomes не загружены");
            errors++;
        }
        else
        {
            foreach (QuestGradeRole role in Enum.GetValues<QuestGradeRole>())
            {
                string key = role.ToString().ToLowerInvariant();
                if (!Outcomes.ContainsKey(key))
                {
                    Log.Error($"[QuestDB] Нет outcome для градации {role}");
                    errors++;
                }
            }

            foreach (var o in Outcomes.Values)
            {
                if (o.DeathChance < 0 || o.DeathChance > 100)
                {
                    Log.Error($"[QuestDB] Outcome '{o.GradeRole}': deathChance вне [0, 100]");
                    errors++;
                }

                if (o.Statuses == null) continue;

                foreach (var s in o.Statuses)
                {
                    if (string.IsNullOrEmpty(s.Id) || !AdventurerDatabase.Statuses.ContainsKey(s.Id))
                    {
                        Log.Error($"[QuestDB] Outcome '{o.GradeRole}': неизвестный статус '{s.Id}'");
                        errors++;
                    }
                    if (s.Chance < 0 || s.Chance > 100)
                    {
                        Log.Error($"[QuestDB] Outcome '{o.GradeRole}': chance у '{s.Id}' вне [0, 100]");
                        errors++;
                    }
                    if (s.DurationDaysMin < 1 || s.DurationDaysMax < s.DurationDaysMin)
                    {
                        Log.Error($"[QuestDB] Outcome '{o.GradeRole}': некорректная длительность '{s.Id}'");
                        errors++;
                    }
                }
            }
        }

        // === Итог ===
        if (errors == 0)
            Log.Info($"[QuestDB] Валидация: ok ({Types.Count} типов, {Grades.Count} градаций, " +
                     $"{Templates.Count} шаблонов, {Creatures.Count} существ, {Locations.Count} локаций, " +
                     $"{Outcomes.Count} исходов)");
        else
            Log.Error($"[QuestDB] Валидация: {errors} ошибок");

        // === Неиспользуемые теги ===
        var used = new HashSet<string>();

        foreach (var t in Templates)
            if (t.Tags != null) foreach (var tag in t.Tags) used.Add(tag);

        foreach (var c in Creatures.Values)
            if (c.Tags != null) foreach (var tag in c.Tags) used.Add(tag);

        foreach (var tag in tagsById.Keys)
            if (!used.Contains(tag))
                Log.Warn($"[QuestDB] Тег '{tag}' не используется");
    }

    private static readonly HashSet<string> ValidCases = new()
    {
        "nom", "gen", "dat", "acc", "ins", "pre"
    };

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

        if (neededCases.Count == 0)
        {
            Log.Info("[QuestDB] Валидация падежей: падежи не используются");
            return;
        }

        int errors = 0;

        foreach (var c in Creatures.Values)
        {
            foreach (var caseCode in neededCases)
            {
                if (string.IsNullOrEmpty(c.Name?.Ru?.Get(caseCode)))
                {
                    Log.Error($"[QuestDB] Creature '{c.Id}': нет формы '{caseCode}'");
                    errors++;
                }
            }
        }

        foreach (var l in Locations.Values)
        {
            foreach (var caseCode in neededCases)
            {
                if (string.IsNullOrEmpty(l.Name?.Ru?.Get(caseCode)))
                {
                    Log.Error($"[QuestDB] Location '{l.Id}': нет формы '{caseCode}'");
                    errors++;
                }
            }
        }

        if (errors == 0)
            Log.Info($"[QuestDB] Валидация падежей: ok ({neededCases.Count} падежей, " +
                     $"{Creatures.Count + Locations.Count} сущностей)");
        else
            Log.Error($"[QuestDB] Валидация падежей: {errors} ошибок");
    }

    // === Загрузка отдельных секций ===

    private static void LoadGrades()
    {
        var gradeList = JsonLoader.Load<QuestGradeDatabase>(
            DataPath + "QuestGrades.json").Grades;

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

    private static void LoadTags()
    {
        var tagList = JsonLoader.Load<QuestTagDatabase>(DataPath + "QuestTags.json").Tags;
        tagsById = tagList.ToDictionary(t => t.Id);

        if (tagsById.Count == 0)
            Log.Warn("[QuestDB] QuestTags.json пуст — все теги будут неизвестны");
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
                // Шаблон без creature — резолвим один раз с пустым creatureId
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

    private static void LoadOutcomes()
    {
        Outcomes = JsonLoader.Load<QuestOutcomeDatabase>(DataPath + "QuestOutcomes.json")
            .Outcomes.ToDictionary(o => o.GradeRole);
    }

    // === Резолв ===

    private static ResolvedTemplate ResolveTemplate(QuestTemplate t, string creatureId)
    {
        var weights = new Dictionary<string, double>();
        QuestTier? tierFromTags = null;

        // 1. Теги шаблона
        MergeTags(t.Tags, weights, ref tierFromTags, $"шаблон '{t.Id}'");

        // 2. Теги creature
        CreatureInfo creature = null;
        if (!string.IsNullOrEmpty(creatureId) && Creatures != null)
            Creatures.TryGetValue(creatureId, out creature);

        if (creature != null)
            MergeTags(creature.Tags, weights, ref tierFromTags, $"creature '{creatureId}'");

        // 3. Override из creature
        if (creature != null)
        {
            ApplyOverride(
                creature.Weights,
                creature.Tier,
                weights,
                ref tierFromTags,
                $"creature '{creatureId}'");
        }

        // 4. Override из шаблона — поверх всего
        ApplyOverride(
            t.Weights,
            t.Tier,
            weights,
            ref tierFromTags,
            $"шаблон '{t.Id}'");

        // 5. Нормировка
        double sum = weights.Values.Sum();
        if (sum <= 0)
        {
            Log.Warn($"[QuestDB] '{t.Id}'+'{creatureId}': нет валидных весов, использую mixed");
            weights = new Dictionary<string, double>
            {
                [StatIds.Strength] = 0.20,
                [StatIds.Dexterity] = 0.20,
                [StatIds.Intelligence] = 0.20,
                [StatIds.Wisdom] = 0.20,
                [StatIds.Charisma] = 0.20,
            };
        }
        else
        {
            foreach (var k in weights.Keys.ToList())
                weights[k] /= sum;
        }

        var tier = tierFromTags ?? QuestTier.Normal;

        return new ResolvedTemplate
        {
            Weights = weights,
            Tier = tier,
            RecommendedPartySize = t.RecommendedPartySize,
        };
    }

    private static void MergeTags(
        List<string> tagIds,
        Dictionary<string, double> weights,
        ref QuestTier? tierFromTags,
        string context)
    {
        if (tagIds == null || tagsById == null) return;

        foreach (var tagId in tagIds)
        {
            if (string.IsNullOrEmpty(tagId)) continue;

            if (!tagsById.TryGetValue(tagId, out var tag))
            {
                Log.Error($"[QuestDB] {context}: неизвестный тег '{tagId}'");
                continue;
            }

            if (tag.Weights != null)
            {
                foreach (var kv in tag.Weights)
                {
                    if (!StatIds.All.Contains(kv.Key))
                    {
                        Log.Error($"[QuestDB] Тег '{tagId}': неизвестный стат '{kv.Key}'");
                        continue;
                    }
                    weights[kv.Key] = weights.GetValueOrDefault(kv.Key, 0) + kv.Value;
                }
            }

            if (tierFromTags == null && !string.IsNullOrEmpty(tag.Tier))
            {
                if (Enum.TryParse<QuestTier>(tag.Tier, ignoreCase: true, out var parsed))
                    tierFromTags = parsed;
                else
                    Log.Error($"[QuestDB] Тег '{tagId}': неизвестный tier '{tag.Tier}'");
            }
        }
    }

    private static void ApplyOverride(Dictionary<string, double> overrideWeights, string overrideTier, Dictionary<string, double> weights, ref QuestTier? tierFromTags, string context)
    {
        if (overrideWeights != null && overrideWeights.Count > 0)
        {
            weights.Clear();
            foreach (var kv in overrideWeights)
            {
                if (!StatIds.All.Contains(kv.Key))
                {
                    Log.Error($"[QuestDB] {context}: неизвестный стат '{kv.Key}'");
                    continue;
                }
                weights[kv.Key] = kv.Value;
            }
        }

        if (!string.IsNullOrEmpty(overrideTier))
        {
            if (Enum.TryParse<QuestTier>(overrideTier, ignoreCase: true, out var parsed))
                tierFromTags = parsed;
            else
                Log.Error($"[QuestDB] {context}: неизвестный tier '{overrideTier}'");
        }
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

        if (resolvedCombos != null &&
            resolvedCombos.TryGetValue((templateId, creatureId), out var r))
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

    public static QuestOutcomeInfo GetOutcome(QuestGradeRole role)
    {
        string key = role.ToString().ToLowerInvariant();
        if (Outcomes != null && Outcomes.TryGetValue(key, out var o)) return o;
        Log.Warn($"[QuestDB] Нет outcome для роли {role}");
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
    public IReadOnlyDictionary<string, double> Weights { get; set; }
    public QuestTier Tier { get; set; }
    public int RecommendedPartySize { get; set; }
}