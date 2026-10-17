namespace AdventurersGuild.Application.Quests;

using System;
using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Application.Reports;
using AdventurersGuild.Core;
using AdventurersGuild.Data.Adventurer;
using AdventurersGuild.Data.Quest;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Common;
using AdventurersGuild.Domain.Quests;

public class QuestFlow(QuestRegistry registry, QuestFactory factory, QuestResolver resolver, GameClock clock, GuildBank bank, ReportsRegistry reports = null, int? seed = null)
{
    private readonly QuestRegistry registry = registry;
    private readonly QuestFactory factory = factory;
    private readonly QuestResolver resolver = resolver;
    private readonly GameClock clock = clock;
    private readonly GuildBank bank = bank;
    private readonly ReportsRegistry reports = reports;
    private readonly Random rng = seed.HasValue ? new Random(seed.Value) : new Random();

    public void Start()
    {
        clock.TimeAdvanced += OnTimeAdvanced;
        RefreshBoard();
    }

    public void Stop() => clock.TimeAdvanced -= OnTimeAdvanced;

    public bool StartQuest(QuestModel quest, IReadOnlyList<UnitModel> party)
    {
        if (quest == null || party == null || party.Count == 0) return false;
        quest.Status = QuestStatus.Pending;
        registry.TakeFromBoard(quest);
        quest.Party = [.. party];
        quest.StartTime = GameTime.NextDayStart(clock.Now);
        quest.LastProcessedAt = quest.StartTime;
        quest.CurrentEncounterIndex = 0;
        quest.DaysSpent = 0;
        quest.Results.Clear();
        return true;
    }

    private void OnTimeAdvanced(GameTime now)
    {
        ExpireOld(now);
        ProcessInProgress(now);
        RefreshBoard();
    }

    private void ExpireOld(GameTime now)
    {
        foreach (var quest in registry.Board.ToList())
        {
            if (now >= quest.ExpiresAt)
            {
                quest.Status = QuestStatus.Expired;
                registry.ExpireToHistory(quest);
            }
        }
    }

    private void ProcessInProgress(GameTime now)
    {
        foreach (var quest in registry.Active.ToList())
        {
            if (quest.Status == QuestStatus.Pending)
            {
                if (now < quest.StartTime) continue;
                quest.Status = QuestStatus.InProgress;
                foreach (var a in quest.Party) a.SetStatus(StatusIds.OnQuest);
            }

            ProcessQuest(quest, now);
        }
    }

    private void ProcessQuest(QuestModel quest, GameTime now)
    {
        // Если все встречи пройдены — завершаем
        if (quest.CurrentEncounterIndex >= quest.Encounters.Count)
        {
            FinishQuest(quest, now);
            return;
        }

        // Логика времени (упрощенная)
        if (quest.CurrentEncounterEndTime.HasValue)
        {
            if (now < quest.CurrentEncounterEndTime.Value) return;

            // Время вышло — разрешаем встречу
            var encounter = quest.Encounters[quest.CurrentEncounterIndex];
            bool passed = ResolveEncounter(quest, encounter);

            quest.Results.Add(new EncounterResult
            {
                Encounter = encounter,
                Passed = passed
            });

            quest.CurrentEncounterEndTime = null;
            quest.CurrentEncounterIndex++;

            // Если провалили главную встречу — досрочное завершение
            if (encounter.IsMainEncounter && !passed)
            {
                quest.CurrentEncounterIndex = quest.Encounters.Count;
            }

            return;
        }

        // Начинаем новую встречу
        var nextEncounter = quest.Encounters[quest.CurrentEncounterIndex];

        // Длительность берем из роли (если есть) или ставим дефолт (1 час)
        int duration = 60;
        if (QuestDatabase.Roles.TryGetValue(nextEncounter.RoleId, out var role) && role.Actions.Count > 0)
        {
            duration = role.Actions[0].Duration;
        }

        quest.CurrentEncounterEndTime = now + GameTime.FromMinutes(duration);
    }

    private bool ResolveEncounter(QuestModel quest, QuestEncounterInstance encounter)
    {
        // 1. Если есть PreCheck (например, Выслеживание), сначала кидаем его
        if (!string.IsNullOrEmpty(encounter.PreCheckSkillId))
        {
            bool preCheckPassed = RollPreCheck(quest, encounter);
            if (preCheckPassed)
            {
                // Если авантюристы выследили волка, засада проваливается автоматически
                return true;
            }
        }

        // 2. Основная проверка (Роль против Авантюриста)
        if (!QuestDatabase.Roles.TryGetValue(encounter.RoleId, out var role)) return true;
        if (role.Actions.Count == 0) return true;

        var action = role.Actions[0];
        if (action.Check == null) return true; // Если проверки нет, считаем успехом

        return RollCombat(quest, encounter, action.Check.Value);
    }

    private bool RollPreCheck(QuestModel quest, QuestEncounterInstance encounter)
    {
        // Ищем лучшего авантюриста по навыку
        var check = new CheckInfo("", encounter.PreCheckSkillId, encounter.PreCheckTargetStat);
        var (bestSkillValue, bestAdv) = FindBestAdventurer(quest.Party, check.TargetSkill, check.TargetStat);

        // Значение NPC (например, внимательность волка = его wis)
        var species = UnitDatabase.GetSpecies(encounter.SpeciesId);
        double npcStat = GetNpcStat(species, encounter.Level, check.TargetStat);

        // Бросок (упрощенная формула: 1d100)
        // Шанс успеха: 50% + разница между навыком и статом NPC
        double diff = bestSkillValue - npcStat;
        int chance = Math.Clamp(50 + (int)(diff * 5), 5, 95);
        int roll = rng.Next(1, 101);

        return roll <= chance;
    }

    private bool RollCombat(QuestModel quest, QuestEncounterInstance encounter, CheckInfo check)
    {
        // NPC бросает свой actorStat
        var species = UnitDatabase.GetSpecies(encounter.SpeciesId);
        double npcValue = GetNpcStat(species, encounter.Level, check.ActorStat);

        // Авантюрист бросает навык (или стат, если навыка нет)
        var (bestSkillValue, bestAdv) = FindBestAdventurer(quest.Party, check.TargetSkill, check.TargetStat);

        // Бросок
        double diff = bestSkillValue - npcValue;
        int chance = Math.Clamp(50 + (int)(diff * 5), 5, 95);
        int roll = rng.Next(1, 101);

        return roll <= chance;
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
                // Формула навыка (стат * модификатор класса)
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

        // Быстрый расчет стата NPC по нашей формуле
        var p = LevelBalance.Active;
        int levelsAboveOne = Math.Max(0, level - 1);
        double weight = species.GrowthWeights.GetValueOrDefault(statId, 0.20);

        double value = p.StatBaseValue + (p.StatStartPool + levelsAboveOne * p.StatBaseSlope) * weight;
        value += species.StatBonuses.GetValueOrDefault(statId, 0);

        return value;
    }

    private void FinishQuest(QuestModel quest, GameTime now)
    {
        quest.Result = resolver.Resolve(quest);
        quest.Status = QuestStatus.Completed;
        registry.CompleteToHistory(quest);
        bank.AddGold(quest.Result.GoldEarned);
        bank.AddGlory(quest.Result.GloryEarned);

        foreach (var a in quest.Party)
        {
            if (!a.HasStatus(StatusIds.Dead)) a.SetStatus(StatusIds.Free);
        }
    }

    private void RefreshBoard()
    {
        while (registry.CountAvailable() < 4)
        {
            // Генерируем квест 1 уровня для партии из 4 человек
            var q = factory.Create(clock.Now, 1, 4);
            if (q != null) registry.Add(q);
            else break;
        }
    }
}