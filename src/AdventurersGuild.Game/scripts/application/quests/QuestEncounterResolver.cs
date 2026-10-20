namespace AdventurersGuild.Application.Quests;

using System;
using System.Collections.Generic;
using AdventurersGuild.Core;
using AdventurersGuild.Domain.Quests;
using AdventurersGuild.Domain.Units;

public class QuestEncounterResolver(Random rng)
{
    public bool Resolve(QuestModel quest, QuestEncounterInstance encounter)
    {
        // 1. Предпроверка (Выслеживание)
        if (!string.IsNullOrEmpty(encounter.PreCheckSkillId))
        {
            if (RollPreCheck(quest, encounter)) return true;
        }

        // 2. Основная проверка роли
        if (!QuestDatabase.Roles.TryGetValue(encounter.RoleId, out var role)) return true;
        if (role.Actions.Count == 0) return true;

        var action = role.Actions[0];
        bool passed = true;

        if (action.Check != null)
        {
            passed = RollCombat(quest, encounter, action.Check.Value);
        }

        // 3. Если провалили — применяем последствия (раны, смерть и т.д.)
        if (!passed && action.Fail?.Consequences != null)
        {
            ApplyConsequences(quest, action.Fail.Consequences);
        }

        return passed;
    }

    private bool RollPreCheck(QuestModel quest, QuestEncounterInstance encounter)
    {
        var check = new CheckInfo("", encounter.PreCheckSkillId, encounter.PreCheckTargetStat);
        var (bestSkillValue, _) = FindBestAdventurer(quest.Party, check.TargetSkill, check.TargetStat);

        var species = UnitDatabase.GetSpecies(encounter.SpeciesId);
        double npcStat = GetNpcStat(species, encounter.Level, check.TargetStat);

        double diff = bestSkillValue - npcStat;
        int chance = Math.Clamp(50 + (int)(diff * 5), 5, 95);
        int roll = rng.Next(1, 101);

        return roll <= chance;
    }

    private bool RollCombat(QuestModel quest, QuestEncounterInstance encounter, CheckInfo check)
    {
        var species = UnitDatabase.GetSpecies(encounter.SpeciesId);
        double npcValue = GetNpcStat(species, encounter.Level, check.ActorStat);

        var (bestSkillValue, _) = FindBestAdventurer(quest.Party, check.TargetSkill, check.TargetStat);

        double diff = bestSkillValue - npcValue;
        int chance = Math.Clamp(50 + (int)(diff * 5), 5, 95);
        int roll = rng.Next(1, 101);

        return roll <= chance;
    }

    private void ApplyConsequences(QuestModel quest, List<ConsequenceInfo> consequences)
    {
        foreach (var adv in quest.Party)
        {
            if (adv.HasStatus(StatusIds.Dead)) continue;

            foreach (var cons in consequences)
            {
                int chance = cons.Chance;

                // Если авантюрист уже ранен, шанс умереть повышается (например, 10% -> 20%)
                if (cons.Id == "death" && adv.HasStatus(StatusIds.Wounded) && cons.IfWounded > 0)
                {
                    chance = cons.IfWounded;
                }

                if (rng.Next(1, 101) <= chance)
                {
                    if (cons.Id == "wound")
                    {
                        adv.SetStatus(StatusIds.Wounded, GameTime.FromDays(3)); // Ранен на 3 дня
                    }
                    else if (cons.Id == "death")
                    {
                        adv.SetStatus(StatusIds.Dead, GameTime.FromDays(4)); // Мертв (удаление через 4 дня)
                    }
                    // "delay" и "dc-penalty" можно добавить позже
                }
            }
        }
    }

    private (double value, UnitModel adv) FindBestAdventurer(List<UnitModel> party, string skillId, string fallbackStat)
    {
        UnitModel bestAdv = null;
        double bestValue = -1;

        foreach (var a in party)
        {
            if (a.HasStatus(StatusIds.Dead)) continue;

            double val = 0;
            if (!string.IsNullOrEmpty(skillId))
            {
                string statId = UnitDatabase.StatForSkill(skillId);
                if (statId != null && a.Stats.TryGetValue(statId, out double statVal))
                {
                    var skill = UnitDatabase.GetSkill(skillId);
                    double mod = 1.0;
                    if (skill != null && skill.ClassModifiers.TryGetValue(a.ClassId, out double m)) mod = m;
                    val = statVal * mod;
                }
            }
            else if (!string.IsNullOrEmpty(fallbackStat))
            {
                val = a.Stats.GetValueOrDefault(fallbackStat, 0);
            }

            if (val > bestValue)
            {
                bestValue = val;
                bestAdv = a;
            }
        }

        return (bestValue, bestAdv);
    }

    private double GetNpcStat(SpeciesInfo species, int level, string statId)
    {
        if (species == null || species.GrowthWeights == null) return 10;

        var p = LevelBalance.Active;
        int levelsAboveOne = Math.Max(0, level - 1);
        double weight = species.GrowthWeights.GetValueOrDefault(statId, 0.20);

        double value = p.StatBaseValue + (p.StatStartPool + levelsAboveOne * p.StatBaseSlope) * weight;
        value += species.StatBonuses.GetValueOrDefault(statId, 0);

        return value;
    }
}