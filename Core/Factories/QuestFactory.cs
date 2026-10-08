using System;
using System.Collections.Generic;

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

        string creatureId = null;
        if (template.Creatures != null && template.Creatures.Count > 0)
            creatureId = Pick(template.Creatures);

        string locationId = null;
        if (template.Locations != null && template.Locations.Count > 0)
            locationId = Pick(template.Locations);

        var resolved = QuestDatabase.GetResolved(template.Id, creatureId);
        if (resolved == null) return null;

        var creature = QuestDatabase.GetCreature(creatureId);
        var location = QuestDatabase.GetLocation(locationId);

        var nouns = new Dictionary<string, LocalizedNoun>
        {
            ["creature"] = creature?.Name,
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
            CreatureId = creatureId,
            Tier = resolved.Tier,
            Phases = resolved.Phases,
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

        string effectiveCreatureId = creatureId;
        if (string.IsNullOrEmpty(effectiveCreatureId))
        {
            if (template.Creatures != null && template.Creatures.Count > 0)
                effectiveCreatureId = template.Creatures[0];
        }
        else if (template.Creatures != null
                 && template.Creatures.Count > 0
                 && !template.Creatures.Contains(effectiveCreatureId))
        {
            Log.Warn($"[QuestFactory] CreateSpecific: существо '{effectiveCreatureId}' " +
                     $"не входит в шаблон '{templateId}', использую как есть");
        }

        string locationId = template.Locations != null && template.Locations.Count > 0
            ? template.Locations[0]
            : null;

        var resolved = QuestDatabase.GetResolved(template.Id, effectiveCreatureId);
        if (resolved == null)
        {
            Log.Error($"[QuestFactory] CreateSpecific: нет resolved для " +
                      $"template='{template.Id}', creature='{effectiveCreatureId}'");
            return null;
        }

        var creature = QuestDatabase.GetCreature(effectiveCreatureId);
        var location = QuestDatabase.GetLocation(locationId);

        var nouns = new Dictionary<string, LocalizedNoun>
        {
            ["creature"] = creature?.Name,
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
            CreatureId = effectiveCreatureId,
            Tier = resolved.Tier,
            Phases = resolved.Phases,
            GoldReward = template.BaseGold,
            GloryReward = template.BaseGlory,
            CreatedAt = now,
            ExpiresAt = now + GameTime.FromDays(lifetimeDays),
            Status = QuestStatus.Available,
        };
    }

    private T Pick<T>(IReadOnlyList<T> list)
    {
        if (list == null || list.Count == 0)
            throw new InvalidOperationException("Pick: пустой список");
        return list[rng.Next(list.Count)];
    }
}