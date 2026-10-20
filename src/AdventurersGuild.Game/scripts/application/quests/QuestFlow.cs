namespace AdventurersGuild.Application.Quests;

using System.Collections.Generic;
using AdventurersGuild.Application.Reports;
using AdventurersGuild.Core;
using AdventurersGuild.Domain.Combat;
using AdventurersGuild.Domain.Common;
using AdventurersGuild.Domain.Quests;
using AdventurersGuild.Domain.Units;

public class QuestFlow(ReportsRegistry reportsRegistry, QuestRegistry questRegistry, QuestBoard questBoard, QuestEncounterResolver resolver, GameClock clock, GuildBank bank)
{
    private readonly ReportsRegistry reportsRegistry = reportsRegistry;
    private readonly QuestRegistry questRegistry = questRegistry;
    private readonly QuestBoard questBoard = questBoard;
    private readonly QuestEncounterResolver resolver = resolver;
    private readonly GameClock clock = clock;
    private readonly GuildBank bank = bank;

    // Добавляем симулятор боя
    private readonly CombatSimulator combatSimulator = new();

    public void Start()
    {
        clock.TimeAdvanced += OnTimeAdvanced;
        questBoard.Refresh(clock.Now);
    }

    public void Stop() => clock.TimeAdvanced -= OnTimeAdvanced;

    public bool StartQuest(QuestModel quest, IReadOnlyList<UnitModel> party)
    {
        if (quest == null || party == null || party.Count == 0) return false;

        quest.Status = QuestStatus.Pending;
        questRegistry.TakeFromBoard(quest);
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
        questBoard.Expire(now);
        ProcessInProgress(now);
        questBoard.Refresh(now);
    }

    private void ProcessInProgress(GameTime now)
    {
        foreach (var quest in questRegistry.Active)
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
        while (true)
        {
            if (quest.CurrentEncounterIndex >= quest.Encounters.Count)
            {
                FinishQuest(quest);
                return;
            }

            var encounter = quest.Encounters[quest.CurrentEncounterIndex];

            RoleInfo role;

            // Если встреча только начинается
            if (!quest.CurrentEncounterEndTime.HasValue)
            {
                int duration = 60;

                if (QuestDatabase.Roles.TryGetValue(encounter.RoleId, out role) && role.Actions.Count > 0)
                {
                    duration = role.Actions[0].Duration;
                }

                quest.CurrentEncounterEndTime = now + GameTime.FromMinutes(duration);
                return; // Ждем времени
            }

            // Если время еще не пришло — выходим
            if (now < quest.CurrentEncounterEndTime.Value)
            {
                return;
            }

            // Время пришло — разрешаем!
            bool passed;

            // Достаем роль из базы
            role = QuestDatabase.Roles[encounter.RoleId];

            // В методе ProcessQuest:
            if (role.Type == EncounterType.Combat)
            {
                var combatResult = CombatSimulator.Simulate(quest, encounter);
                passed = combatResult.PartyWon;

                ApplyWounds(quest, combatResult.AdventurerWounds);

                var enemy = UnitDatabase.GetSpecies(encounter.SpeciesId);
                string enemyName = enemy?.Name?.Get("ru") ?? encounter.SpeciesId;

                // Формируем официальный рапорт
                string combatLogText = CombatFormatter.FormatBattle(combatResult.Events, enemyName, encounter.Level);

                // Отправляем в окно отчетов (без Log.Info!)
                reportsRegistry?.Append(clock.Now.Day, quest, combatLogText);
            }
            else
            {
                // Для не боевых ролей используем старую систему проверок (торговцы, паломники)
                passed = resolver.Resolve(quest, encounter);

                // НАРРАТИВ: Красивое описание социальной встречи через новый класс
                if (reportsRegistry != null)
                {
                    string socialText = SocialFormatter.FormatEncounter(encounter.RoleId, passed);
                    reportsRegistry.Append(clock.Now.Day, quest, socialText);
                }
            }

            quest.Results.Add(new EncounterResult
            {
                Encounter = encounter,
                Passed = passed
            });

            quest.CurrentEncounterEndTime = null;
            quest.CurrentEncounterIndex++;

            // Если провалили главную встречу — квест сразу окончен
            if (encounter.IsMainEncounter && !passed)
            {
                quest.CurrentEncounterIndex = quest.Encounters.Count;
            }
        }
    }

    private static void ApplyWounds(QuestModel quest, Dictionary<string, WoundLevel> wounds)
    {
        foreach (var adv in quest.Party)
        {
            if (wounds.TryGetValue(adv.Id, out var woundLevel))
            {
                if (woundLevel == WoundLevel.Dead)
                {
                    adv.SetStatus(StatusIds.Dead, GameTime.FromDays(4));
                }
                else if (woundLevel == WoundLevel.Heavy)
                {
                    adv.SetStatus("wound_heavy", GameTime.FromDays(5));
                }
                else if (woundLevel == WoundLevel.Medium)
                {
                    adv.SetStatus("wound_medium", GameTime.FromDays(3));
                }
                else if (woundLevel == WoundLevel.Light)
                {
                    adv.SetStatus("wound_light", GameTime.FromDays(1));
                }
            }
        }
    }

    private void FinishQuest(QuestModel quest)
    {
        quest.Status = QuestStatus.Completed;
        questRegistry.CompleteToHistory(quest);

        foreach (var a in quest.Party)
        {
            if (!a.HasStatus(StatusIds.Dead)) a.SetStatus(StatusIds.Free);
        }
    }
}