using System;
using System.Collections.Generic;
using Godot;

public enum Gender { Male, Female }

public class AdventurerFactory(int? seed = null)
{
    private readonly Random rng = seed.HasValue ? new Random(seed.Value) : new Random();

    public AdventurerModel Create(string templateId = "recruit")
    {
        var template = FindTemplate(templateId);

        if (template == null)
        {
            if (AdventurerDatabase.Templates.Count == 0)
            {
                GD.PushError("[AdventurerFactory] Нет шаблонов");
                return null;
            }
            template = AdventurerDatabase.Templates[0];
        }

        var raceId = PickRaceByPopulation();
        var race = AdventurerDatabase.GetRace(raceId);

        if (race == null) return null;

        var classId = PickByWeight(race.Weight.Classes);

        if (classId == null) return null;

        var level = rng.Next(template.MinLevel, template.MaxLevel + 1);
        var stats = StatGrowth.AtLevel(classId, raceId, level);

        var gender = rng.Next(2) == 0 ? Gender.Male : Gender.Female;
        var firstPool = gender == Gender.Male ? AdventurerDatabase.Names.FirstNamesMale : AdventurerDatabase.Names.FirstNamesFemale;

        var first = Pick(firstPool).Get(Loc.Language);
        var last = Pick(AdventurerDatabase.Names.Surnames).Get(Loc.Language);
        var id = Guid.NewGuid().ToString("N");

        return new AdventurerModel(id, first, last, classId, raceId, stats, level);
    }

    private string PickRaceByPopulation()
    {
        var total = 0;

        foreach (var r in AdventurerDatabase.Races.Values)
            total += r.Weight?.Population ?? 0;

        if (total <= 0)
        {
            GD.PushError("[AdventurerFactory] Нет рас с population > 0");
            return null;
        }

        var roll = rng.Next(total);
        var acc = 0;

        foreach (var r in AdventurerDatabase.Races.Values)
        {
            acc += r.Weight?.Population ?? 0;
            if (roll < acc) return r.Id;
        }

        return null;
    }

    private AdventurerTemplateInfo FindTemplate(string id)
    {
        foreach (var t in AdventurerDatabase.Templates)
            if (t.Id == id) return t;
        return null;
    }

    private string PickByWeight(Dictionary<string, int> weights)
    {
        if (weights == null || weights.Count == 0)
        {
            GD.PushError("[AdventurerFactory] PickByWeight: пустой словарь");
            return null;
        }

        var total = 0;

        foreach (var w in weights.Values) total += w;

        if (total <= 0) return null;

        var roll = rng.Next(total);
        var acc = 0;

        foreach (var kv in weights)
        {
            acc += kv.Value;
            if (roll < acc) return kv.Key;
        }

        // fallback — первый ненулевой
        foreach (var kv in weights)
            if (kv.Value > 0) return kv.Key;

        return null;
    }

    private LocalizedName Pick(List<LocalizedName> pool)
    {
        if (pool == null || pool.Count == 0)
            return default;
        return pool[rng.Next(pool.Count)];
    }
}