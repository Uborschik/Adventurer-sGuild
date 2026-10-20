namespace AdventurersGuild.Application.Adventurers;

using System;
using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Core;
using AdventurersGuild.Core.Localization;
using AdventurersGuild.Domain.Game;
using AdventurersGuild.Domain.Units;

public enum Gender { Male, Female }

public class UnitFactory(int? seed = null)
{
    private readonly Random rng = seed.HasValue ? new Random(seed.Value) : new Random();

    public UnitModel Create()
    {
        var species = PickRandomAdventurerSpecies();

        if (species == null) return null;

        (var classId, var level) = PickClassAndLevel(species);

        if (classId == null) return null;

        var stats = StatGrowth.AtLevel(classId, species.Id, level);

        var gender = rng.Next(2) == 0 ? Gender.Male : Gender.Female;
        var firstPool = gender == Gender.Male ? UnitDatabase.Names.FirstNamesMale : UnitDatabase.Names.FirstNamesFemale;

        var first = Pick(firstPool).Get(Loc.Language);
        var last = Pick(UnitDatabase.Names.Surnames).Get(Loc.Language);
        var id = Guid.NewGuid().ToString("N");

        var model = new UnitModel(id, first, last, classId, species.Id, stats, level);

        return model;
    }

    private SpeciesInfo PickRandomAdventurerSpecies()
    {
        var allowedIds = GameRules.AdventurerSpecies;

        if (allowedIds == null || allowedIds.Count == 0) return null;

        var pool = new List<SpeciesInfo>();
        var totalWeight = 0;

        foreach (var id in allowedIds)
        {
            SpeciesInfo species = UnitDatabase.GetSpecies(id);

            if (species != null && species.Population > 0)
            {
                pool.Add(species);
                totalWeight += species.Population;
            }
        }

        if (totalWeight <= 0) return null;

        var roll = rng.Next(totalWeight);
        var acc = 0;

        foreach (SpeciesInfo species in pool)
        {
            acc += species.Population;

            if (roll < acc) return species;
        }

        return pool[0];
    }

    private (string classId, int level) PickClassAndLevel(SpeciesInfo species)
    {
        if (species.Classes == null || species.Classes.Count == 0) return (null, 0);

        var targetLvl = GameRules.InitialAdventurerLvl;

        var validClasses = species.Classes
            .Where(c => targetLvl >= c.MinLvl && targetLvl <= c.MaxLvl)
            .ToList();

        if (validClasses.Count == 0)
            validClasses = [.. species.Classes];

        var total = 0;

        foreach (SpeciesClassInfo c in validClasses) total += c.Weight;

        if (total <= 0) return (null, 0);

        var roll = rng.Next(total);
        var acc = 0;
        SpeciesClassInfo chosen = null;

        foreach (SpeciesClassInfo c in validClasses)
        {
            acc += c.Weight;
            if (roll < acc)
            {
                chosen = c;
                break;
            }
        }

        if (chosen == null) return (null, 0);

        return (chosen.ClassId, targetLvl);
    }

    private LocalizedName Pick(List<LocalizedName> pool)
    {
        if (pool == null || pool.Count == 0) return default;
        return pool[rng.Next(pool.Count)];
    }

    private static void LogAdventurer(UnitModel a, string classId, string raceId)
    {
        var stats = string.Join(" ", StatIds.All.Select(s => $"{s}={a.Stats[s]:F2}"));
        Log.Info($"[Adv] \n {raceId}/{classId} \n {a.FullName} \n L{a.Level.Number}  {stats}");
    }
}