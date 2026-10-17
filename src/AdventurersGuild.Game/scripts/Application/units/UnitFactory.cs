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

    public UnitModel Create()
    {
        // 1. Выбираем вид (Species) по популяции, но только из разрешенных
        var species = PickRandomAdventurerSpecies();
        if (species == null) return null;

        // 2. Выбираем класс и уровень
        var (classId, level) = PickClassAndLevel(species);
        if (classId == null) return null;

        // 3. Считаем статы и генерируем имя
        var stats = StatGrowth.AtLevel(classId, species.Id, level);

        var gender = rng.Next(2) == 0 ? Gender.Male : Gender.Female;
        var firstPool = gender == Gender.Male ? UnitDatabase.Names.FirstNamesMale : UnitDatabase.Names.FirstNamesFemale;

        var first = Pick(firstPool).Get(Loc.Language);
        var last = Pick(UnitDatabase.Names.Surnames).Get(Loc.Language);
        var id = Guid.NewGuid().ToString("N");

        var model = new UnitModel(id, first, last, classId, species.Id, stats, level);

        LogAdventurer(model, classId, species.Id);

        return model;
    }

    private SpeciesInfo PickRandomAdventurerSpecies()
    {
        var allowedIds = UnitDatabase.GameRules?.AdventurerSpecies;
        if (allowedIds == null || allowedIds.Count == 0) return null;

        // Собираем только тех, кто есть в разрешенном списке
        var pool = new List<SpeciesInfo>();
        int totalWeight = 0;

        foreach (var id in allowedIds)
        {
            var species = UnitDatabase.GetSpecies(id);
            if (species != null && species.Population > 0)
            {
                pool.Add(species);
                totalWeight += species.Population;
            }
        }

        if (totalWeight <= 0) return null;

        // Крутим рулетку по Population
        int roll = rng.Next(totalWeight);
        int acc = 0;

        foreach (var species in pool)
        {
            acc += species.Population;
            if (roll < acc) return species;
        }

        return pool[0];
    }

    private (string classId, int level) PickClassAndLevel(SpeciesInfo species)
    {
        if (species.Classes == null || species.Classes.Count == 0) return (null, 0);

        int total = 0;
        foreach (var c in species.Classes) total += c.Weight;

        if (total <= 0) return (null, 0);

        int roll = rng.Next(total);
        int acc = 0;
        SpeciesClassInfo chosen = null;

        foreach (var c in species.Classes)
        {
            acc += c.Weight;
            if (roll < acc)
            {
                chosen = c;
                break;
            }
        }

        if (chosen == null) return (null, 0);

        int level = rng.Next(chosen.MinLvl, chosen.MaxLvl + 1);
        return (chosen.ClassId, level);
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