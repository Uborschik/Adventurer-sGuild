namespace AdventurersGuild.Domain.Combat;

using System;
using System.Collections.Generic;
using AdventurersGuild.Domain.Quests;
using AdventurersGuild.Domain.Units;

public static class CombatActorFactory
{
    public static CombatActor FromAdventurer(UnitModel adv)
    {
        var cls = UnitDatabase.GetClass(adv.ClassId);
        var rules = LevelBalance.Active.Combat;
        string atkStatId = cls?.PrimaryStat ?? "str";

        double dex = adv.Stats.GetValueOrDefault("dex", rules.DefaultStatFallback);
        double end = adv.Stats.GetValueOrDefault("end", rules.DefaultStatFallback);

        return new CombatActor
        {
            Name = adv.FirstName,
            OriginalAdventurer = adv,
            IsEnemy = false,
            MaxHp = adv.MaxHp,
            CurrentHp = adv.CurrentHp,
            Attack = adv.Stats.GetValueOrDefault(atkStatId, rules.DefaultStatFallback),
            Initiative = dex,
            Ac = CombatMath.CalculateAc(dex, end)
        };
    }

    public static List<CombatActor> FromEncounter(QuestEncounterInstance encounter)
    {
        var enemies = new List<CombatActor>();
        if (encounter.Units == null) return enemies;

        var rules = LevelBalance.Active;

        foreach (var unit in encounter.Units)
        {
            var species = UnitDatabase.GetSpecies(unit.SpeciesId);
            var enemyClass = UnitDatabase.GetClass(unit.ClassId);

            if (species == null || enemyClass == null) continue;

            string enemyName = species.Name?.Get("ru") ?? unit.SpeciesId;
            string atkStatId = enemyClass.PrimaryStat ?? "str";

            double enemyEnd = GetNpcStat(species, unit.Level, "end");
            double enemyDex = GetNpcStat(species, unit.Level, "dex");
            double enemyAtk = GetNpcStat(species, unit.Level, atkStatId);

            double baseHp = enemyEnd * rules.HpPerEndurance;
            int maxHp = (int)(baseHp * enemyClass.HpMultiplier);

            enemies.Add(new CombatActor
            {
                Name = $"{enemyName} ({UnitDatabase.ClassName(unit.ClassId)})",
                IsEnemy = true,
                MaxHp = maxHp,
                CurrentHp = maxHp,
                Attack = enemyAtk,
                Initiative = enemyDex,
                Ac = CombatMath.CalculateAc(enemyDex, enemyEnd)
            });
        }

        return enemies;
    }

    private static double GetNpcStat(SpeciesInfo species, int level, string statId)
    {
        if (species == null || species.GrowthWeights == null) return 10;
        var p = LevelBalance.Active;
        int levelsAboveOne = Math.Max(0, level - 1);
        double weight = species.GrowthWeights.GetValueOrDefault(statId, p.Combat.DefaultGrowthWeight);

        double value = p.StatBaseValue + (p.StatStartPool + levelsAboveOne * p.StatBaseSlope) * weight;
        value += species.StatBonuses.GetValueOrDefault(statId, 0);
        return value;
    }
}