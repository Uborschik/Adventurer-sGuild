using System;
using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Core;
using AdventurersGuild.Core.Localization;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Data.Quest;
using AdventurersGuild.Domain.Quests;
using AdventurersGuild.Infrastructure;

namespace AdventurersGuild.Application.Quests;

public class QuestFactory
{
    private readonly Random rng;
    private readonly List<QuestTemplate> templates;

    public QuestFactory(int? seed = null)
    {
        rng = seed.HasValue ? new Random(seed.Value) : new Random();

        var db = JsonLoader.Load<QuestTemplateDatabase>(
            "res://Resources/Data/Quest/QuestTemplates.json");
        templates = db.Templates;
    }

    public QuestModel Create(GameTime now, int codeLevel)
    {
        var template = Pick(templates);

        string locationId = null;
        if (template.Locations != null && template.Locations.Count > 0)
            locationId = Pick(template.Locations);
        var location = QuestDatabase.GetLocation(locationId);

        var chosenCreatures = PickCreatures(template, location);

        var phases = QuestDatabase.ResolveQuest(template, location, chosenCreatures, codeLevel);

        var nouns = new Dictionary<string, LocalizedNoun>
        {
            ["creature"] = chosenCreatures.FirstOrDefault()?.Name,
            ["location"] = location?.Name,
        };

        string lang = Loc.Language;
        string description = TextSubstitution.Resolve(
            template.Description.Get(lang), lang, nouns);

        var lifetimeDays = rng.Next(3, 8);
        int narrativeLevel = (codeLevel - 1) / 10 + 1;

        return new QuestModel
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = template.Name.Get(lang) ?? template.Id,
            CodeLevel = codeLevel,
            NarrativeLevel = narrativeLevel,
            Description = description,
            TypeId = template.Type,
            LocationId = locationId,
            CreatureId = chosenCreatures.FirstOrDefault()?.Id,
            DifficultyModifier = template.DifficultyModifier + (location?.DifficultyModifier ?? 0),
            Phases = phases,
            GoldReward = template.BaseGold,
            GloryReward = template.BaseGlory,
            CreatedAt = now,
            ExpiresAt = now + GameTime.FromDays(lifetimeDays),
            Status = QuestStatus.Available,
        };
    }

    public QuestModel CreateSpecific(GameTime now, int codeLevel, string templateId, string creatureId)
    {
        var template = templates.Find(t => t.Id == templateId);
        if (template == null)
        {
            Log.Error($"[QuestFactory] CreateSpecific: шаблон '{templateId}' не найден");
            return null;
        }

        string locationId = template.Locations != null && template.Locations.Count > 0
            ? template.Locations[0]
            : null;
        var location = QuestDatabase.GetLocation(locationId);

        // Если задан creatureId — берём только его. Иначе — все из пула шаблона + локации.
        List<CreatureInfo> chosen;
        if (!string.IsNullOrEmpty(creatureId))
        {
            chosen = new List<CreatureInfo>();
            var c = QuestDatabase.GetCreature(creatureId);
            if (c != null) chosen.Add(c);
        }
        else
        {
            chosen = PickCreatures(template, location);
        }

        var phases = QuestDatabase.ResolveQuest(template, location, chosen, codeLevel);

        var nouns = new Dictionary<string, LocalizedNoun>
        {
            ["creature"] = chosen.FirstOrDefault()?.Name,
            ["location"] = location?.Name,
        };

        string lang = Loc.Language;
        string description = TextSubstitution.Resolve(
            template.Description.Get(lang), lang, nouns);

        const int lifetimeDays = 5;
        int narrativeLevel = (codeLevel - 1) / 10 + 1;

        return new QuestModel
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = template.Name.Get(lang) ?? template.Id,
            CodeLevel = codeLevel,
            NarrativeLevel = narrativeLevel,
            Description = description,
            TypeId = template.Type,
            LocationId = locationId,
            CreatureId = chosen.FirstOrDefault()?.Id,
            DifficultyModifier = template.DifficultyModifier + (location?.DifficultyModifier ?? 0),
            Phases = phases,
            GoldReward = template.BaseGold,
            GloryReward = template.BaseGlory,
            CreatedAt = now,
            ExpiresAt = now + GameTime.FromDays(lifetimeDays),
            Status = QuestStatus.Available,
        };
    }

    private List<CreatureInfo> PickCreatures(QuestTemplate template, LocationInfo location)
    {
        var pool = new HashSet<string>();
        if (template.Creatures != null)
            foreach (var c in template.Creatures)
                if (!string.IsNullOrEmpty(c)) pool.Add(c);
        if (location?.Creatures != null)
            foreach (var c in location.Creatures)
                if (!string.IsNullOrEmpty(c)) pool.Add(c);

        var poolList = pool.ToList();
        if (poolList.Count == 0) return new List<CreatureInfo>();

        var creatureId = poolList[rng.Next(poolList.Count)];
        var creature = QuestDatabase.GetCreature(creatureId);

        return creature != null
            ? new List<CreatureInfo> { creature }
            : new List<CreatureInfo>();
    }

    private T Pick<T>(IReadOnlyList<T> list)
    {
        if (list == null || list.Count == 0)
            throw new InvalidOperationException("Pick: пустой список");
        return list[rng.Next(list.Count)];
    }
}