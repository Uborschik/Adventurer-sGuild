namespace AdventurersGuild.Domain.Quests;

using System.Collections.Generic;
using AdventurersGuild.Application.Quests;
using AdventurersGuild.Core;
using AdventurersGuild.Domain.Adventurers;

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
    // === Идентичность ===
    public string Id { get; set; }
    public string Name { get; set; }

    // === Классификация ===
    public string BlueprintId { get; set; }
    public string TargetId { get; set; }
    public string LocationId { get; set; }

    public int CodeLevel { get; set; }
    public int NarrativeLevel { get; set; }

    // === Текст и сложность ===
    public string Description { get; set; }
    public double DifficultyModifier { get; set; }

    // === Фазы ===
    public List<QuestPhaseInstance> Phases { get; set; } = new();
    public List<QuestPhaseInstance> AmbientPhases { get; set; } = new();
    public string StartRoleId { get; set; }

    // === Прогресс ===
    public List<PhaseResult> PhaseResults { get; set; } = new();
    public string CurrentRoleId { get; set; }
    public int AmbientIndex { get; set; }
    public int DaysSpent { get; set; }
    public int MinutesUsedToday { get; set; }
    public GameTime LastProcessedAt { get; set; }

    // === Текущая фаза ===
    public GameTime? CurrentPhaseEndTime { get; set; }

    // === Ночь ===
    public bool IsResting { get; set; }
    public GameTime? RestEndTime { get; set; }

    // === Награды ===
    public int GoldReward { get; set; }
    public int GloryReward { get; set; }

    // === Жизненный цикл ===
    public GameTime CreatedAt { get; set; }
    public GameTime ExpiresAt { get; set; }
    public QuestStatus Status { get; set; } = QuestStatus.Available;

    // === Партия ===
    public List<AdventurerModel> Party { get; set; }
    public GameTime StartTime { get; set; }

    // === Финал ===
    public QuestResult Result { get; set; }
}