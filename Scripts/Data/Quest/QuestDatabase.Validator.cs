namespace AdventurersGuild.Data.Quest;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AdventurersGuild.Core;
using AdventurersGuild.Data.Adventurer;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Quests;

public static partial class QuestDatabase
{
    private static void Validate()
    {
        int errors = 0;

        errors += ValidateTags();
        errors += ValidateGrades();
        errors += ValidatePhases();
        errors += ValidateCreatures();
        errors += ValidateLocations();
        errors += ValidateBlueprints();
        errors += ValidateDeclensions();

        if (errors == 0)
            Log.Info($"[QuestDB] Валидация: ok " +
                     $"({Blueprints.Count} blueprint'ов, {Tags.Count} тегов, " +
                     $"{Phases.Count} фаз, {Creatures.Count} существ, " +
                     $"{Locations.Count} локаций, {Grades.Count} градаций)");
        else
            Log.Error($"[QuestDB] Валидация: {errors} ошибок");
    }

    // === Tags ===

    private static int ValidateTags()
    {
        int errors = 0;
        foreach (var tag in Tags.Values)
        {
            if (string.IsNullOrEmpty(tag.Id))
            {
                Log.Error("[QuestDB] Тег без id");
                errors++;
            }
            if (string.IsNullOrEmpty(tag.Name.Ru) || string.IsNullOrEmpty(tag.Name.En))
            {
                Log.Error($"[QuestDB] Тег '{tag.Id}': неполное name");
                errors++;
            }
        }
        return errors;
    }

    // === Grades ===

    private static int ValidateGrades()
    {
        int errors = 0;
        foreach (var g in Grades)
        {
            if (string.IsNullOrEmpty(g.Name.Ru) || string.IsNullOrEmpty(g.Name.En))
            {
                Log.Error($"[QuestDB] Градация '{g.Role}': неполное name");
                errors++;
            }
        }
        return errors;
    }

    // === Phases ===

    private static int ValidatePhases()
    {
        int errors = 0;
        foreach (var p in Phases.Values)
        {
            if (string.IsNullOrEmpty(p.Id))
            {
                Log.Error("[QuestDB] Фаза без id");
                errors++;
                continue;
            }

            foreach (var tag in p.Tags)
            {
                if (!Tags.ContainsKey(tag))
                {
                    Log.Error($"[QuestDB] Фаза '{p.Id}': неизвестный тег '{tag}'");
                    errors++;
                }
            }

            if (p.Paths == null || p.Paths.Count == 0)
            {
                Log.Error($"[QuestDB] Фаза '{p.Id}': пустой paths");
                errors++;
                continue;
            }

            foreach (var path in p.Paths)
            {
                if (path.Checks == null || path.Checks.Count == 0)
                {
                    Log.Error($"[QuestDB] Фаза '{p.Id}': пустой path");
                    errors++;
                    continue;
                }

                foreach (var check in path.Checks)
                {
                    if (string.IsNullOrEmpty(check.Skill))
                    {
                        Log.Error($"[QuestDB] Фаза '{p.Id}': check без skill");
                        errors++;
                        continue;
                    }

                    if (!AdventurerDatabase.IsValidSkill(check.Skill))
                    {
                        Log.Error($"[QuestDB] Фаза '{p.Id}': неизвестный навык '{check.Skill}'");
                        errors++;
                    }

                    if (check.Resistance == null || check.Resistance.Count == 0)
                    {
                        Log.Error($"[QuestDB] Фаза '{p.Id}': check '{check.Skill}' без resistance");
                        errors++;
                        continue;
                    }

                    foreach (var statId in check.Resistance)
                    {
                        if (!StatIds.All.Contains(statId))
                        {
                            Log.Error($"[QuestDB] Фаза '{p.Id}': неизвестный стат '{statId}' " +
                                      $"в resistance для '{check.Skill}'");
                            errors++;
                        }
                    }
                }
            }
        }
        return errors;
    }

    // === Creatures ===

    private static int ValidateCreatures()
    {
        int errors = 0;
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
            else
            {
                foreach (var statId in StatIds.All)
                {
                    if (!c.GrowthWeights.ContainsKey(statId))
                        Log.Warn($"[QuestDB] Creature '{c.Id}': нет веса для '{statId}', будет 0");
                    else if (c.GrowthWeights[statId] < 0)
                    {
                        Log.Error($"[QuestDB] Creature '{c.Id}': отрицательный вес '{statId}'");
                        errors++;
                    }
                }

                foreach (var kv in c.GrowthWeights)
                {
                    if (!StatIds.All.Contains(kv.Key))
                    {
                        Log.Error($"[QuestDB] Creature '{c.Id}': неизвестный стат '{kv.Key}' в growthWeights");
                        errors++;
                    }
                }
            }

            if (c.Contributions == null || c.Contributions.Count == 0)
            {
                Log.Error($"[QuestDB] Creature '{c.Id}': нет contributions");
                errors++;
                continue;
            }

            foreach (var contribution in c.Contributions)
            {
                if (string.IsNullOrEmpty(contribution.PhaseId))
                {
                    Log.Error($"[QuestDB] Creature '{c.Id}': contribution без phaseId");
                    errors++;
                    continue;
                }

                if (!Phases.TryGetValue(contribution.PhaseId, out var phase))
                {
                    Log.Error($"[QuestDB] Creature '{c.Id}': contribution на неизвестную " +
                              $"фазу '{contribution.PhaseId}'");
                    errors++;
                    continue;
                }

                if (contribution.Tags == null || contribution.Tags.Count == 0)
                {
                    Log.Error($"[QuestDB] Creature '{c.Id}': contribution '{contribution.PhaseId}' без tags");
                    errors++;
                    continue;
                }

                foreach (var tag in contribution.Tags)
                {
                    if (!Tags.ContainsKey(tag))
                    {
                        Log.Error($"[QuestDB] Creature '{c.Id}': contribution '{contribution.PhaseId}' " +
                                  $"использует неизвестный тег '{tag}'");
                        errors++;
                    }
                    else if (!phase.Tags.Contains(tag))
                    {
                        Log.Error($"[QuestDB] Creature '{c.Id}': contribution '{contribution.PhaseId}' " +
                                  $"использует тег '{tag}', которого нет у фазы");
                        errors++;
                    }
                }
            }
        }
        return errors;
    }

    // === Locations ===

    private static int ValidateLocations()
    {
        int errors = 0;
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
        return errors;
    }

    // === Blueprints ===

    private static int ValidateBlueprints()
    {
        int errors = 0;
        foreach (var bp in Blueprints.Values)
        {
            if (string.IsNullOrEmpty(bp.Name.Ru) || string.IsNullOrEmpty(bp.Name.En))
            {
                Log.Error($"[QuestDB] Blueprint '{bp.Id}': неполное name");
                errors++;
            }

            if (bp.Roles == null || bp.Roles.Count == 0)
            {
                Log.Error($"[QuestDB] Blueprint '{bp.Id}': нет ролей");
                errors++;
                continue;
            }

            // Уникальность id ролей
            var seenIds = new HashSet<string>();
            foreach (var role in bp.Roles)
            {
                if (!seenIds.Add(role.Id))
                {
                    Log.Error($"[QuestDB] Blueprint '{bp.Id}': дубликат роли '{role.Id}'");
                    errors++;
                }
            }

            // Теги роли существуют
            foreach (var role in bp.Roles)
            {
                if (role.Tags == null || role.Tags.Count == 0)
                {
                    Log.Error($"[QuestDB] Blueprint '{bp.Id}': роль '{role.Id}' без тегов");
                    errors++;
                    continue;
                }

                foreach (var tag in role.Tags)
                {
                    if (!Tags.ContainsKey(tag))
                    {
                        Log.Error($"[QuestDB] Blueprint '{bp.Id}': роль '{role.Id}' " +
                                  $"использует неизвестный тег '{tag}'");
                        errors++;
                    }
                }
            }

            // Все Incoming.From существуют
            var byId = bp.Roles.ToDictionary(r => r.Id);
            foreach (var role in bp.Roles)
            {
                foreach (var edge in role.Incoming)
                {
                    if (!byId.ContainsKey(edge.From))
                    {
                        Log.Error($"[QuestDB] Blueprint '{bp.Id}': роль '{role.Id}' " +
                                  $"ссылается на несуществующую '{edge.From}'");
                        errors++;
                    }
                }
            }

            // Start: ровно одна роль без incoming
            var starts = bp.Roles.Where(r => r.Incoming.Count == 0).ToList();
            if (starts.Count != 1)
            {
                Log.Error($"[QuestDB] Blueprint '{bp.Id}': " +
                          $"{starts.Count} стартовых ролей (нужна ровно 1)");
                errors++;
                continue;
            }
            var start = starts[0];

            // End: минимум одна роль без outgoing
            var ends = bp.Roles.Where(r => r.Outgoing.Count == 0).ToList();
            if (ends.Count == 0)
            {
                Log.Error($"[QuestDB] Blueprint '{bp.Id}': нет end-ролей");
                errors++;
                continue;
            }

            // DAG: нет циклов
            if (HasCycle(bp, start.Id))
            {
                Log.Error($"[QuestDB] Blueprint '{bp.Id}': граф содержит цикл");
                errors++;
                continue;
            }

            // Все роли достижимы из start
            var reachable = ReachableFrom(bp, start.Id);
            foreach (var role in bp.Roles)
            {
                if (!reachable.Contains(role.Id))
                {
                    Log.Error($"[QuestDB] Blueprint '{bp.Id}': роль '{role.Id}' " +
                              $"недостижима из start");
                    errors++;
                }
            }

            // Каждая required-роль доминирует все end
            foreach (var role in bp.Roles)
            {
                if (!role.Required) continue;

                foreach (var end in ends)
                {
                    if (!IsDominator(bp, role.Id, end.Id))
                    {
                        Log.Error($"[QuestDB] Blueprint '{bp.Id}': required-роль '{role.Id}' " +
                                  $"не доминирует end '{end.Id}'");
                        errors++;
                    }
                }
            }
        }
        return errors;
    }

    // === Helpers ===

    private static bool HasCycle(Blueprint bp, string startId)
    {
        var byId = bp.Roles.ToDictionary(r => r.Id);
        var visiting = new HashSet<string>();
        var done = new HashSet<string>();

        bool Dfs(string id)
        {
            if (done.Contains(id)) return false;
            if (!visiting.Add(id)) return true;

            foreach (var edge in byId[id].Outgoing)
                if (Dfs(edge.To)) return true;

            visiting.Remove(id);
            done.Add(id);
            return false;
        }

        return Dfs(startId);
    }

    private static HashSet<string> ReachableFrom(Blueprint bp, string startId)
    {
        var byId = bp.Roles.ToDictionary(r => r.Id);
        var visited = new HashSet<string>();
        var stack = new Stack<string>();
        stack.Push(startId);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current)) continue;

            foreach (var edge in byId[current].Outgoing)
                stack.Push(edge.To);
        }
        return visited;
    }

    private static bool IsDominator(Blueprint bp, string dominatorId, string endId)
    {
        if (dominatorId == endId) return true;

        var byId = bp.Roles.ToDictionary(r => r.Id);
        var start = bp.Roles.First(r => r.Incoming.Count == 0);

        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(start.Id);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == dominatorId) continue;
            if (current == endId) return false;

            foreach (var edge in byId[current].Outgoing)
            {
                if (visited.Add(edge.To))
                    queue.Enqueue(edge.To);
            }
        }
        return true;
    }

    // === Падежи ===

    private static readonly HashSet<string> ValidCases =
        new() { "nom", "gen", "dat", "acc", "ins", "pre" };

    private static int ValidateDeclensions()
    {
        var neededCases = new HashSet<string>();
        var pattern = new Regex(@"\{(\w+):(\w+)\}");

        foreach (var bp in Blueprints.Values)
        {
            string ru = bp.Description.Ru;
            if (string.IsNullOrEmpty(ru)) continue;

            foreach (Match m in pattern.Matches(ru))
            {
                string caseCode = m.Groups[2].Value;
                if (!ValidCases.Contains(caseCode))
                {
                    Log.Error($"[QuestDB] Blueprint '{bp.Id}': неизвестный падеж '{caseCode}'");
                    continue;
                }
                neededCases.Add(caseCode);
            }
        }

        if (neededCases.Count == 0) return 0;

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

        return errors;
    }
}