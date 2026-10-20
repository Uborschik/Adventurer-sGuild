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

    public static CombatActor FromEnemy(QuestEncounterInstance encounter)
    {
        var species = UnitDatabase.GetSpecies(encounter.SpeciesId);
        var enemyClass = UnitDatabase.GetClass(encounter.ClassId);
        var rules = LevelBalance.Active;

        string enemyName = species?.Name?.Get("ru") ?? encounter.SpeciesId;
        string atkStatId = enemyClass?.PrimaryStat ?? "str";

        double enemyEnd = GetNpcStat(species, encounter.Level, "end");
        double enemyDex = GetNpcStat(species, encounter.Level, "dex");
        double enemyAtk = GetNpcStat(species, encounter.Level, atkStatId);

        // Формула здоровья без hpBase (как ты и решил)
        double baseHp = enemyEnd * rules.HpPerEndurance;
        int maxHp = (int)(baseHp * (enemyClass?.HpMultiplier ?? 1.0));

        return new CombatActor
        {
            Name = enemyName,
            IsEnemy = true,
            MaxHp = maxHp,
            CurrentHp = maxHp,
            Attack = enemyAtk,
            Initiative = enemyDex,
            Ac = CombatMath.CalculateAc(enemyDex, enemyEnd)
        };
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