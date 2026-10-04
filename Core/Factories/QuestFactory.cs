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

    public QuestModel Create(GameTime now)
    {
        var template = Pick(templates);

        int min = template.LevelRange?.Min ?? 1;
        int max = template.LevelRange?.Max ?? min;
        if (max < min) max = min;
        int level = rng.Next(min, max + 1);

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

        return new QuestModel
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = template.Name.Get(lang) ?? template.Id,
            Level = level,
            Description = description,
            TypeId = template.Type,

            Tier = resolved.Tier,
            Weights = resolved.Weights,
            RecommendedPartySize = resolved.RecommendedPartySize,

            DurationMinutes = template.DurationDays * GameTime.MinutesPerDay,
            GoldReward = template.BaseGold,
            GloryReward = template.BaseGlory,

            CreatedAt = now,
            ExpiresAt = now + GameTime.FromDays(lifetimeDays),
        };
    }

    private T Pick<T>(IReadOnlyList<T> list)
    {
        if (list == null || list.Count == 0)
            throw new InvalidOperationException("Pick: пустой список");
        return list[rng.Next(list.Count)];
    }
}