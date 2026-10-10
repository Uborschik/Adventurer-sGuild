namespace AdventurersGuild.Application.Quests;

using System;
using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Core;
using AdventurersGuild.Core.Localization;
using AdventurersGuild.Data.Balance;
using AdventurersGuild.Data.Compendium;
using AdventurersGuild.Data.Quest;
using AdventurersGuild.Domain.Quests;
using AdventurersGuild.Infrastructure;

public class QuestFactory
{
    private readonly QuestGenerator generator;
    private readonly IReadOnlyList<Blueprint> blueprints;
    private readonly Random rng;

    public QuestFactory(
        QuestGenerator generator,
        IReadOnlyList<Blueprint> blueprints,
        int? seed = null)
    {
        this.generator = generator ?? throw new ArgumentNullException(nameof(generator));
        this.blueprints = blueprints ?? throw new ArgumentNullException(nameof(blueprints));
        rng = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    // === Создание ===

    public QuestModel Create(GameTime now, int codeLevel)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            var blueprint = PickRandom(blueprints);
            if (blueprint == null) continue;

            var target = PickTargetForBlueprint(blueprint, codeLevel);
            if (target == null) continue;

            var location = PickLocationForTarget(target);
            if (location == null) continue;

            var ctx = BuildContext(blueprint, target, location, codeLevel);
            var result = generator.Generate(ctx, rng);
            if (result == null) continue;

            return BuildQuestModel(now, codeLevel, ctx, result);
        }

        Log.Warn("[QuestFactory] Не удалось создать квест за 10 попыток");
        return null;
    }

    public QuestModel CreateSpecific(
        GameTime now,
        int codeLevel,
        string blueprintId,
        string targetId)
    {
        var blueprint = blueprints.FirstOrDefault(b => b.Id == blueprintId);
        if (blueprint == null)
        {
            Log.Error($"[QuestFactory] Blueprint '{blueprintId}' не найден");
            return null;
        }

        var target = QuestDatabase.GetCreature(targetId);
        if (target == null)
        {
            Log.Error($"[QuestFactory] Target '{targetId}' не найден");
            return null;
        }

        var location = PickLocationForTarget(target);
        if (location == null)
        {
            Log.Error($"[QuestFactory] Локация для target '{targetId}' не найдена");
            return null;
        }

        var ctx = BuildContext(blueprint, target, location, codeLevel);
        var result = generator.Generate(ctx, rng);
        if (result == null) return null;

        return BuildQuestModel(now, codeLevel, ctx, result);
    }

    // === Выбор ===

    private Blueprint PickRandom(IReadOnlyList<Blueprint> items)
    {
        if (items == null || items.Count == 0) return null;
        return items[rng.Next(items.Count)];
    }

    private CreatureInfo PickTargetForBlueprint(Blueprint blueprint, int codeLevel)
    {
        var candidates = QuestDatabase.Creatures.Values
            .Where(c => codeLevel >= c.MinLvl && codeLevel <= c.MaxLvl)
            .Where(c => CanCoverRequiredRoles(c, blueprint))
            .ToList();

        if (candidates.Count == 0) return null;
        return candidates[rng.Next(candidates.Count)];
    }

    private static bool CanCoverRequiredRoles(CreatureInfo creature, Blueprint blueprint)
    {
        if (creature.Contributions == null || creature.Contributions.Count == 0)
            return false;

        // Грубая проверка: для каждой required-роли существует хоть один
        // contribution, чьи теги пересекаются с тегами роли.
        foreach (var role in blueprint.Roles)
        {
            if (!role.Required) continue;

            bool any = creature.Contributions.Any(c =>
                c.Tags != null && c.Tags.Any(t => role.Tags.Contains(t)));

            if (!any) return false;
        }
        return true;
    }

    private LocationInfo PickLocationForTarget(CreatureInfo target)
    {
        var bestiary = CompendiumDatabase.Bestiary;
        if (bestiary?.Habitats == null) return null;

        var locations = bestiary.Habitats
            .Where(h => h.Creatures != null &&
                        h.Creatures.Any(c => c.CreatureId == target.Id))
            .Select(h => QuestDatabase.GetLocation(h.LocationId))
            .Where(l => l != null)
            .ToList();

        if (locations.Count == 0) return null;
        return locations[rng.Next(locations.Count)];
    }

    private IReadOnlyList<AmbientCandidate> BuildAmbientPool(LocationInfo location, CreatureInfo target)
    {
        var bestiary = CompendiumDatabase.Bestiary;
        if (bestiary?.Habitats == null) return Array.Empty<AmbientCandidate>();

        var habitat = bestiary.Habitats.FirstOrDefault(h => h.LocationId == location.Id);
        if (habitat?.Creatures == null) return Array.Empty<AmbientCandidate>();

        var result = new List<AmbientCandidate>();
        foreach (var entry in habitat.Creatures)
        {
            if (entry.CreatureId == target.Id) continue;

            var creature = QuestDatabase.GetCreature(entry.CreatureId);
            if (creature == null) continue;

            result.Add(new AmbientCandidate(creature, entry.EffectiveWeight));
        }
        return result;
    }

    // === Контекст и модель ===

    private QuestContext BuildContext(
        Blueprint blueprint,
        CreatureInfo target,
        LocationInfo location,
        int codeLevel)
    {
        return new QuestContext
        {
            Blueprint = blueprint,
            Target = target,
            Location = location,
            AmbientPool = BuildAmbientPool(location, target),
            CodeLevel = codeLevel,
            AdventurerBalance = AdventurerBalance.Active,
        };
    }

    private QuestModel BuildQuestModel(
        GameTime now,
        int codeLevel,
        QuestContext ctx,
        QuestGenerationResult result)
    {
        var creatureNoun = ctx.Target.Name;
        var locationNoun = ctx.Location.Name;

        var nouns = new Dictionary<string, LocalizedNoun>
        {
            ["target"] = creatureNoun,
            ["creature"] = creatureNoun, // совместимость со старыми шаблонами
            ["location"] = locationNoun,
        };

        string lang = Loc.Language;
        string description = TextSubstitution.Resolve(
            ctx.Blueprint.Description.Get(lang), lang, nouns);

        int lifetimeDays = 3 + rng.Next(5);
        int narrativeLevel = (codeLevel - 1) / 10 + 1;

        return new QuestModel
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = ctx.Blueprint.Name.Get(lang) ?? ctx.Blueprint.Id,
            BlueprintId = ctx.Blueprint.Id,
            TargetId = ctx.Target.Id,
            LocationId = ctx.Location.Id,
            CodeLevel = codeLevel,
            NarrativeLevel = narrativeLevel,
            Description = description,
            DifficultyModifier = ctx.Blueprint.DifficultyModifier + ctx.Location.DifficultyModifier,
            Phases = result.Phases,
            AmbientPhases = result.AmbientPhases,
            StartRoleId = result.StartRoleId,
            GoldReward = ctx.Blueprint.BaseGold,
            GloryReward = ctx.Blueprint.BaseGlory,
            CreatedAt = now,
            ExpiresAt = now + GameTime.FromDays(lifetimeDays),
            Status = QuestStatus.Available,
        };
    }
}