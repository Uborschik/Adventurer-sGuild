using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AdventurersGuild.Core;
using AdventurersGuild.Data.Adventurer;
using AdventurersGuild.Domain.Adventurers;

namespace AdventurersGuild.Data.Quest;

public static partial class QuestDatabase
{
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

        // === Фазы ===
        foreach (var p in Phases.Values)
        {
            if (string.IsNullOrEmpty(p.Id))
            {
                Log.Error("[QuestDB] Фаза без id");
                errors++;
                continue;
            }

            if (p.IsMarker)
            {
                // Маркеры: должны иметь paths с checks.
                // У маркеров нет ни solutions, ни expReward (они приходят от существ).
                if (p.Paths == null || p.Paths.Count == 0)
                {
                    Log.Error($"[QuestDB] Маркер '{p.Id}': пустой paths");
                    errors++;
                    continue;
                }
                foreach (var path in p.Paths)
                {
                    if (path.Checks == null || path.Checks.Count == 0)
                    {
                        Log.Error($"[QuestDB] Маркер '{p.Id}': пустой path");
                        errors++;
                        continue;
                    }
                    foreach (var check in path.Checks)
                    {
                        if (string.IsNullOrEmpty(check.Skill))
                        {
                            Log.Error($"[QuestDB] Маркер '{p.Id}': check без skill");
                            errors++;
                            continue;
                        }
                        if (!AdventurerDatabase.IsValidSkill(check.Skill))
                        {
                            Log.Error($"[QuestDB] Маркер '{p.Id}': неизвестный навык '{check.Skill}'");
                            errors++;
                        }
                        if (check.Resistance == null || check.Resistance.Count == 0)
                        {
                            Log.Error($"[QuestDB] Маркер '{p.Id}': check '{check.Skill}' без resistance");
                            errors++;
                            continue;
                        }
                        foreach (var statId in check.Resistance)
                        {
                            if (!StatIds.All.Contains(statId))
                            {
                                Log.Error($"[QuestDB] Маркер '{p.Id}': неизвестный стат '{statId}' " +
                                          $"в resistance для '{check.Skill}'");
                                errors++;
                            }
                        }
                    }
                }
            }
            else
            {
                // Standalone: должны иметь solutions с DC > 0.
                // У short_rest исключение — фаза без solutions.
                if (p.Solutions == null || p.Solutions.Count == 0)
                {
                    if (p.Id != "short_rest")
                    {
                        Log.Error($"[QuestDB] Standalone '{p.Id}': пустой solutions");
                        errors++;
                    }
                    continue;
                }

                if (p.ExpReward < 0)
                {
                    Log.Error($"[QuestDB] Standalone '{p.Id}': expReward < 0");
                    errors++;
                }

                foreach (var sol in p.Solutions)
                {
                    if (sol.Skills == null || sol.Skills.Count == 0)
                    {
                        Log.Error($"[QuestDB] Standalone '{p.Id}': пустое решение");
                        errors++;
                        continue;
                    }
                    foreach (var kv in sol.Skills)
                    {
                        if (!AdventurerDatabase.IsValidSkill(kv.Key))
                        {
                            Log.Error($"[QuestDB] Standalone '{p.Id}': неизвестный навык '{kv.Key}'");
                            errors++;
                        }
                        if (kv.Value <= 0)
                        {
                            Log.Error($"[QuestDB] Standalone '{p.Id}': DC <= 0 у '{kv.Key}'");
                            errors++;
                        }
                    }
                }
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

            // Requires — список маркеров
            if (t.Requires == null || t.Requires.Count == 0)
            {
                Log.Error($"[QuestDB] Шаблон '{t.Id}': пустой requires");
                errors++;
            }
            else
            {
                foreach (var req in t.Requires)
                {
                    if (!Phases.TryGetValue(req, out var phase))
                    {
                        Log.Error($"[QuestDB] Шаблон '{t.Id}': фаза '{req}' не найдена");
                        errors++;
                    }
                    else if (!phase.IsMarker)
                    {
                        Log.Error($"[QuestDB] Шаблон '{t.Id}': '{req}' в requires, " +
                                  $"но это не маркер");
                        errors++;
                    }
                }
            }

            // Дополнительные существа (поверх локаций)
            if (t.Creatures != null)
                foreach (var c in t.Creatures)
                    if (string.IsNullOrEmpty(c) || !Creatures.ContainsKey(c))
                    {
                        Log.Error($"[QuestDB] Шаблон '{t.Id}': creature '{c}' не найден");
                        errors++;
                    }

            // Локации
            if (t.Locations == null || t.Locations.Count == 0)
            {
                Log.Error($"[QuestDB] Шаблон '{t.Id}': нет локаций");
                errors++;
            }
            else
            {
                foreach (var l in t.Locations)
                    if (string.IsNullOrEmpty(l) || !Locations.ContainsKey(l))
                    {
                        Log.Error($"[QuestDB] Шаблон '{t.Id}': location '{l}' не найден");
                        errors++;
                    }
            }
        }

        // === Существа ===
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
            if (c.MaxLvl < c.MinLvl)
            {
                Log.Error($"[QuestDB] Creature '{c.Id}': maxLvl < minLvl");
                errors++;
            }
            if (c.GrowthWeights == null || c.GrowthWeights.Count == 0)
            {
                Log.Error($"[QuestDB] Creature '{c.Id}': пустой growthWeights");
                errors++;
            }
            if (c.Promises == null || c.Promises.Count == 0)
            {
                Log.Error($"[QuestDB] Creature '{c.Id}': нет promises");
                errors++;
            }
            else
            {
                foreach (var promise in c.Promises)
                {
                    if (string.IsNullOrEmpty(promise.Phase))
                    {
                        Log.Error($"[QuestDB] Creature '{c.Id}': promise без phase");
                        errors++;
                        continue;
                    }
                    if (!Phases.TryGetValue(promise.Phase, out var phase))
                    {
                        Log.Error($"[QuestDB] Creature '{c.Id}': promise на неизвестную " +
                                  $"фазу '{promise.Phase}'");
                        errors++;
                    }
                    else if (!phase.IsMarker)
                    {
                        Log.Error($"[QuestDB] Creature '{c.Id}': promise на '{promise.Phase}', " +
                                  $"но это не маркер");
                        errors++;
                    }
                }
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

            if (l.Standalone != null)
                foreach (var sid in l.Standalone)
                {
                    if (!Phases.TryGetValue(sid, out var phase))
                    {
                        Log.Error($"[QuestDB] Location '{l.Id}': standalone '{sid}' не найден");
                        errors++;
                    }
                    else if (phase.IsMarker)
                    {
                        Log.Error($"[QuestDB] Location '{l.Id}': '{sid}' в standalone, " +
                                  $"но это маркер");
                        errors++;
                    }
                }

            if (l.Creatures != null)
                foreach (var cid in l.Creatures)
                {
                    if (!Creatures.ContainsKey(cid))
                    {
                        Log.Error($"[QuestDB] Location '{l.Id}': creature '{cid}' не найден");
                        errors++;
                    }
                }
        }

        if (errors == 0)
            Log.Info($"[QuestDB] Валидация: ok ({Types.Count} типов, {Grades.Count} градаций, " +
                     $"{Phases.Count} фаз, {Templates.Count} шаблонов, " +
                     $"{Creatures.Count} существ, {Locations.Count} локаций)");
        else
            Log.Error($"[QuestDB] Валидация: {errors} ошибок");
    }

    private static readonly HashSet<string> ValidCases =
        new() { "nom", "gen", "dat", "acc", "ins", "pre" };

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