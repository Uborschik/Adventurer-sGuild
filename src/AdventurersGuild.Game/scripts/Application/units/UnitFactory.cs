namespace AdventurersGuild.Application.Adventurers;

using System;
using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Core;
using AdventurersGuild.Core.Localization;
using AdventurersGuild.Domain.Adventurers;

public enum Gender { Male, Female }

public class UnitFactory(int? seed = null)
{
    private readonly Random rng = seed.HasValue ? new Random(seed.Value) : new Random();

    public UnitModel Create(string raceId, string classId, int lvl)
    {
        var stats = StatGrowth.AtLevel(classId, raceId, lvl);

        var gender = rng.Next(2) == 0 ? Gender.Male : Gender.Female;
        var firstPool = gender == Gender.Male ? UnitDatabase.Names.FirstNamesMale : UnitDatabase.Names.FirstNamesFemale;

        var first = Pick(firstPool).Get(Loc.Language);
        var last = Pick(UnitDatabase.Names.Surnames).Get(Loc.Language);
        var id = Guid.NewGuid().ToString("N");

        var model = new UnitModel(id, first, last, classId, raceId, stats, lvl);

        LogAdventurer(model, classId, raceId);

        return model;
    }

    public UnitModel Create(string templateId = "recruit")
    {
        var template = FindTemplate(templateId);

        if (template == null)
        {
            if (UnitDatabase.Templates.Count == 0)
            {
                Log.Error("[UnitFactory] Нет шаблонов");
                return null;
            }
            template = UnitDatabase.Templates[0];
        }

        var raceId = PickRaceByPopulation();
        var race = UnitDatabase.GetRace(raceId);

        if (race == null) return null;

        var classId = PickByWeight(race.Weight.Classes);

        if (classId == null) return null;

        // var level = rng.Next(template.MinLevel, template.MaxLevel + 1);
        var level = 1;
        var stats = StatGrowth.AtLevel(classId, raceId, level);

        var gender = rng.Next(2) == 0 ? Gender.Male : Gender.Female;
        var firstPool = gender == Gender.Male ? UnitDatabase.Names.FirstNamesMale : UnitDatabase.Names.FirstNamesFemale;

        var first = Pick(firstPool).Get(Loc.Language);
        var last = Pick(UnitDatabase.Names.Surnames).Get(Loc.Language);
        var id = Guid.NewGuid().ToString("N");

        return new UnitModel(id, first, last, classId, raceId, stats, level);
    }

    private string PickRaceByPopulation()
    {
        var total = 0;

        foreach (var r in UnitDatabase.Races.Values)
            total += r.Weight?.Population ?? 0;

        if (total <= 0)
        {
            Log.Error("[UnitFactory] Нет рас с population > 0");
            return null;
        }

        var roll = rng.Next(total);
        var acc = 0;

        foreach (var r in UnitDatabase.Races.Values)
        {
            acc += r.Weight?.Population ?? 0;
            if (roll < acc) return r.Id;
        }

        return null;
    }

    private UnitTemplateInfo FindTemplate(string id)
    {
        foreach (var t in UnitDatabase.Templates)
            if (t.Id == id) return t;
        return null;
    }

    private string PickByWeight(Dictionary<string, int> weights)
    {
        if (weights == null || weights.Count == 0)
        {
            Log.Error("[UnitFactory] PickByWeight: пустой словарь");
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

    private static void LogAdventurer(UnitModel a, string classId, string raceId)
    {
        var stats = string.Join(" ",
            StatIds.All.Select(s => $"{s}={a.Stats[s]:F2}"));

        Log.Info($"[Adv] \n {raceId}/{classId} \n {a.FullName} \n L{a.Level.Number}  {stats}");
    }
}