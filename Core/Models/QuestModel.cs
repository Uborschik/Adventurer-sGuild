using System.Collections.Generic;
using static QuestCalculator;

public enum QuestStatus
{
    Available,
    InProgress,
    Completed,
    Expired
}

public class QuestModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Level { get; set; } = 1;
    public string Description { get; set; }
    public string TypeId { get; set; }
    public int Difficulty { get; set; }
    public IReadOnlyDictionary<string, double> Weights { get; set; }
    public QuestTier Tier { get; set; }
    public int RecommendedPartySize { get; set; }
    public int DurationMinutes { get; set; }
    public int GoldReward { get; set; }
    public int GloryReward { get; set; }
    public GameTime CreatedAt { get; set; }
    public GameTime ExpiresAt { get; set; }

    public QuestStatus Status { get; set; } = QuestStatus.Available;

    public List<AdventurerModel> Party { get; set; }
    public List<AdventurerModel> DeadAdventurers { get; set; } = new();
    public GameTime StartTime { get; set; }
    public GameTime EndTime { get; set; }

    public QuestResult Result { get; set; }
}