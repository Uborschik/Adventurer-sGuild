namespace AdventurersGuild.Application.Quests;

using System;
using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Core;
using AdventurersGuild.Data.Adventurer;
using AdventurersGuild.Domain.Quests;

public class QuestGenerator
{
    private readonly IReadOnlyDictionary<string, QuestPhaseTemplate> phases;

    public QuestGenerator(IReadOnlyDictionary<string, QuestPhaseTemplate> phases)
    {
        this.phases = phases ?? throw new ArgumentNullException(nameof(phases));
    }

    // === Точка входа ===

    public QuestGenerationResult Generate(QuestContext ctx, Random rng)
    {
        if (ctx == null || rng == null) return null;

        var targetStats = new CreatureStats(ctx.Target, ctx.CodeLevel, ctx.AdventurerBalance);

        var fills = FillRoles(ctx, targetStats);
        if (fills == null) return null;

        var result = new QuestGenerationResult
        {
            StartRoleId = ctx.Blueprint.Roles.FirstOrDefault(r => r.Incoming.Count == 0)?.Id,
            Phases = BuildPhases(ctx, fills),
        };

        AddAmbientPhases(ctx, result.AmbientPhases, rng);

        ApplyDifficulty(ctx, result);

        ValidateResult(ctx, result);

        return result;
    }

    // === Заполнение ролей ===

    private sealed class RoleFill
    {
        public BlueprintRole Role;
        public CreatureContribution Contribution;
        public CreatureStats Stats;
    }

    private Dictionary<string, RoleFill> FillRoles(QuestContext ctx, CreatureStats targetStats)
    {
        var used = new HashSet<CreatureContribution>();
        var fills = new Dictionary<string, RoleFill>();

        foreach (var role in ctx.Blueprint.Roles)
        {
            var candidates = ctx.Target.Contributions
                .Where(c => !used.Contains(c))
                .Where(c => c.Tags != null && c.Tags.Any(t => role.Tags.Contains(t)))
                .ToList();

            if (candidates.Count == 0)
            {
                if (role.Required)
                {
                    Log.Warn($"[QuestGenerator] Blueprint '{ctx.Blueprint.Id}', target '{ctx.Target.Id}': " +
                             $"required-роль '{role.Id}' не покрыта ни одним contribution");
                    return null;
                }
                continue;
            }

            var chosen = PickByWeight(candidates, c => c.Weight, ctx, role.Id, "");
            used.Add(chosen);

            fills[role.Id] = new RoleFill
            {
                Role = role,
                Contribution = chosen,
                Stats = targetStats,
            };
        }

        return fills;
    }

    // === Построение фаз ===

    private List<QuestPhaseInstance> BuildPhases(
        QuestContext ctx,
        Dictionary<string, RoleFill> fills)
    {
        var included = new HashSet<string>(fills.Keys);
        var byId = ctx.Blueprint.Roles.ToDictionary(r => r.Id);
        var result = new List<QuestPhaseInstance>();

        foreach (var role in ctx.Blueprint.Roles)
        {
            if (!included.Contains(role.Id)) continue;

            var fill = fills[role.Id];

            if (!phases.TryGetValue(fill.Contribution.PhaseId, out var template))
            {
                Log.Error($"[QuestGenerator] Фаза '{fill.Contribution.PhaseId}' не найдена");
                continue;
            }

            var solutions = BuildSolutions(template, fill.Stats, ctx.ReferenceMaxStat);

            var instance = new QuestPhaseInstance
            {
                RoleId = role.Id,
                PhaseId = template.Id,
                Name = template.Name,
                Solutions = solutions,
                DurationMinutes = fill.Contribution.Duration,
                ExpReward = fill.Contribution.Exp,
                Critical = role.Required,
                NextOnPass = ResolveTransition(role, EdgeCondition.Pass, included, byId),
                NextOnFail = ResolveTransition(role, EdgeCondition.Fail, included, byId),
            };

            result.Add(instance);
        }

        return result;
    }

    private static QuestTransition ResolveTransition(BlueprintRole role, EdgeCondition condition, HashSet<string> included, Dictionary<string, BlueprintRole> byId)
    {
        foreach (var edge in role.Outgoing)
        {
            if (edge.Condition != condition && edge.Condition != EdgeCondition.Any)
                continue;

            if (included.Contains(edge.To))
                return new QuestTransition(edge.To, edge.DcDelta, edge.DurationDeltaMinutes);

            if (byId.TryGetValue(edge.To, out var skipped))
            {
                var via = ResolveTransition(skipped, condition, included, byId);
                if (via != default) return via;
            }
        }
        return default;
    }

    // === Ambient ===

    private void AddAmbientPhases(QuestContext ctx, List<QuestPhaseInstance> output, Random rng)
    {
        int min = ctx.Blueprint.AmbientRange.Min;
        int max = ctx.Blueprint.AmbientRange.Max;
        if (max <= 0) return;

        int count = rng.Next(min, max + 1);
        if (count <= 0) return;

        var pool = ctx.AmbientPool;
        if (pool == null || pool.Count == 0) return;

        for (int i = 0; i < count; i++)
        {
            var candidate = PickByWeight(pool, c => c.Weight, ctx, "", "ambient");
            if (candidate.Creature?.Contributions == null || candidate.Creature.Contributions.Count == 0)
                continue;

            var contribution = PickByWeight(
                candidate.Creature.Contributions,
                c => c.Weight, ctx, "", "ambient-contribution");

            if (!phases.TryGetValue(contribution.PhaseId, out var template)) continue;

            var stats = new CreatureStats(candidate.Creature, ctx.CodeLevel, ctx.AdventurerBalance);
            var solutions = BuildSolutions(template, stats, ctx.ReferenceMaxStat);

            output.Add(new QuestPhaseInstance
            {
                RoleId = null,
                PhaseId = template.Id,
                Name = template.Name,
                Solutions = solutions,
                DurationMinutes = contribution.Duration,
                ExpReward = contribution.Exp,
                Critical = false,
                NextOnPass = default,
                NextOnFail = default,
            });
        }
    }

    // === Solutions ===

    private List<PhaseSolution> BuildSolutions(QuestPhaseTemplate template, CreatureStats stats, double refMax)
    {
        var result = new List<PhaseSolution>();

        if (template.Paths == null) return result;

        foreach (var path in template.Paths)
        {
            var solution = new PhaseSolution { Skills = new() };

            if (path.Checks != null)
            {
                foreach (var check in path.Checks)
                {
                    double dc = ComputeCheckDc(check, stats, refMax);
                    if (dc >= 0)
                        solution.Skills[check.Skill] = dc;
                }
            }

            if (solution.Skills.Count > 0)
                result.Add(solution);
        }

        return result;
    }

    private static double ComputeCheckDc(PhaseCheck check, CreatureStats stats, double refMax)
    {
        if (refMax <= 0) return -1;

        if (check.Resistance == null || check.Resistance.Count == 0)
        {
            string statId = AdventurerDatabase.StatForSkill(check.Skill);
            if (statId == null) return -1;
            return stats.Get(statId) / refMax;
        }

        double sum = 0;
        int count = 0;
        foreach (var statId in check.Resistance)
        {
            sum += stats.Get(statId);
            count++;
        }

        if (count == 0) return -1;
        return (sum / count) / refMax;
    }

    // === Difficulty ===

    private static void ApplyDifficulty(QuestContext ctx, QuestGenerationResult result)
    {
        double difficulty = ctx.Blueprint.DifficultyModifier + ctx.Location.DifficultyModifier;
        if (Math.Abs(difficulty) < 1e-9) return;

        foreach (var phase in result.Phases)
        {
            foreach (var sol in phase.Solutions)
            {
                var keys = new List<string>(sol.Skills.Keys);
                foreach (var k in keys)
                    sol.Skills[k] += difficulty;
            }
        }

        // ambient тоже страдает от difficulty, но не от blueprint (там только локация)
        if (Math.Abs(ctx.Location.DifficultyModifier) > 1e-9)
        {
            foreach (var phase in result.AmbientPhases)
            {
                foreach (var sol in phase.Solutions)
                {
                    var keys = new List<string>(sol.Skills.Keys);
                    foreach (var k in keys)
                        sol.Skills[k] += ctx.Location.DifficultyModifier;
                }
            }
        }
    }

    // === Валидация результата ===

    private static void ValidateResult(QuestContext ctx, QuestGenerationResult result)
    {
        if (result.Phases.Count == 0)
        {
            Log.Warn($"[QuestGenerator] Blueprint '{ctx.Blueprint.Id}': 0 фаз в результате");
            return;
        }

        bool hasCritical = result.Phases.Any(p => p.Critical);
        if (!hasCritical)
            Log.Warn($"[QuestGenerator] Blueprint '{ctx.Blueprint.Id}': нет критичных фаз");

        foreach (var phase in result.Phases)
        {
            if (phase.Solutions.Count == 0)
                Log.Warn($"[QuestGenerator] Роль '{phase.RoleId}': фаза '{phase.PhaseId}' " +
                         $"имеет пустые solutions");
        }
    }

    // === Выбор по весу ===

    private static T PickByWeight<T>(
        IReadOnlyList<T> items,
        Func<T, int> getWeight,
        QuestContext ctx,
        string roleId,
        string context)
    {
        if (items == null || items.Count == 0) return default;

        int total = 0;
        foreach (var item in items)
        {
            int w = getWeight(item);
            if (w > 0) total += w;
        }

        if (total <= 0)
        {
            // все веса 0 или отрицательные — равномерно
            return items[GetRngFromCtx(ctx).Next(items.Count)];
        }

        int roll = GetRngFromCtx(ctx).Next(total);
        int acc = 0;
        foreach (var item in items)
        {
            int w = getWeight(item);
            if (w <= 0) continue;
            acc += w;
            if (roll < acc) return item;
        }
        return items[^1];
    }

    // Заглушка — на этапе 4 используем глобальный Random, потом заменим
    private static Random _fallbackRng = new();
    private static Random GetRngFromCtx(QuestContext ctx)
    {
        // Позже заменим: ctx.Rng или прокинуть Random через параметры.
        return _fallbackRng;
    }
}