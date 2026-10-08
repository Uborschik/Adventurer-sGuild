using System.Collections.Generic;
using System.Text.RegularExpressions;

public static partial class QuestDatabase
{
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
}