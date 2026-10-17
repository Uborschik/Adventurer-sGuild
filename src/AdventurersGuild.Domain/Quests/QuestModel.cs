namespace AdventurersGuild.Domain.Quests;

using System.Collections.Generic;
using AdventurersGuild.Core;
using AdventurersGuild.Domain.Adventurers;

public enum QuestStatus { Available, Pending, InProgress, Completed, Expired }

public class QuestModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    public string TemplateId { get; set; }
    public string LocationId { get; set; }
    public int CodeLevel { get; set; }

    // Новый линейный список
    public List<QuestEncounterInstance> Encounters { get; set; } = [];
    public int CurrentEncounterIndex { get; set; }

    // Прогресс
    public List<EncounterResult> Results { get; set; } = [];
    public int DaysSpent { get; set; }
    public int MinutesUsedToday { get; set; }
    public GameTime LastProcessedAt { get; set; }
    public GameTime? CurrentEncounterEndTime { get; set; }

    public bool IsResting { get; set; }
    public GameTime? RestEndTime { get; set; }

    // Награды
    public int GoldReward { get; set; }
    public int GloryReward { get; set; }

    // Жизненный цикл
    public GameTime CreatedAt { get; set; }
    public GameTime ExpiresAt { get; set; }
    public QuestStatus Status { get; set; } = QuestStatus.Available;

    // Партия и финал
    public List<UnitModel> Party { get; set; }
    public GameTime StartTime { get; set; }
    public QuestResult Result { get; set; }
}

public class QuestEncounterInstance
{
    public string SpeciesId { get; set; }
    public string RoleId { get; set; }
    public int Level { get; set; }

    // Главное столкновение?
    public bool IsMainEncounter { get; set; }

    // Данные для предпроверки (если есть)
    public string PreCheckSkillId { get; set; }
    public string PreCheckTargetStat { get; set; }
}

public class EncounterResult
{
    public QuestEncounterInstance Encounter { get; set; }
    public bool Passed { get; set; }
    public string ActionId { get; set; }
}