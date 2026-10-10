using System.Collections.Generic;
using AdventurersGuild.Application.Quests;
using AdventurersGuild.Core;
using AdventurersGuild.Domain.Adventurers;

namespace AdventurersGuild.Domain.Quests;

public enum QuestStatus
{
    Available,
    Pending,
    InProgress,
    Completed,
    Expired
}

public class QuestModel
{
    public string Id { get; set; }
    public string Name { get; set; }

    public int CodeLevel { get; set; }
    public int NarrativeLevel { get; set; }

    public string Description { get; set; }
    public string TypeId { get; set; }
    public string LocationId { get; set; }
    public string CreatureId { get; set; }

    public double DifficultyModifier { get; set; }
    public List<QuestPhaseTemplate> Phases { get; set; } = new();

    // Прогресс
    public List<PhaseResult> PhaseResults { get; set; } = new();
    public int CurrentPhaseIndex { get; set; }
    public int DaysSpent { get; set; }
    public int MinutesUsedToday { get; set; }
    public GameTime LastProcessedAt { get; set; }

    // Текущая фаза в процессе
    public GameTime? CurrentPhaseEndTime { get; set; }

    // Ночь
    public bool IsResting { get; set; }
    public GameTime? RestEndTime { get; set; }

    public int GoldReward { get; set; }
    public int GloryReward { get; set; }

    public GameTime CreatedAt { get; set; }
    public GameTime ExpiresAt { get; set; }

    public QuestStatus Status { get; set; } = QuestStatus.Available;

    public List<AdventurerModel> Party { get; set; }
    public GameTime StartTime { get; set; }

    public QuestResult Result { get; set; }
}