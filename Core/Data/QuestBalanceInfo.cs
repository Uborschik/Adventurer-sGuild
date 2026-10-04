using System.Collections.Generic;

public struct TierBonusMap
{
    public double Easy { get; set; }
    public double Normal { get; set; }
    public double Hard { get; set; }

    public double Get(QuestTier tier) => tier switch
    {
        QuestTier.Easy => Easy,
        QuestTier.Normal => Normal,
        QuestTier.Hard => Hard,
        _ => 0
    };
}

public class QuestBalanceProfile
{
    public string Id { get; set; }

    public int BasePenalty { get; set; } = 20;
    public int ReferenceStatAtLevel1 { get; set; } = 6;
    public double CoverageThreshold { get; set; } = 0.75;
    public double CoverageBonusPerStat { get; set; } = 0.25;

    public TierBonusMap TierBonus { get; set; } = new();

    public double MarginTriumph { get; set; } = 50;
    public double MarginSuccess { get; set; } = 0;
    public double MarginFailure { get; set; } = -40;

    public double TriumphCapPercent { get; set; } = 10;

    public ExperienceBalance Experience { get; set; } = new();
}

public class QuestBalanceDatabase
{
    public List<QuestBalanceProfile> Profiles { get; set; } = new();
}

public class ExperienceBalance
{
    public int ExpBonusLvl1 { get; set; } = 5;
    public TierModifierMap TierModifiers { get; set; } = new();
    public RoleMultiplierMap RoleMultipliers { get; set; } = new();
    public double ContributionBaseScore { get; set; } = 0.2;
}

public class TierModifierMap
{
    public double Easy { get; set; }
    public double Normal { get; set; }
    public double Hard { get; set; }

    public double Get(QuestTier tier) => tier switch
    {
        QuestTier.Easy => Easy,
        QuestTier.Normal => Normal,
        QuestTier.Hard => Hard,
        _ => 0
    };
}

public class RoleMultiplierMap
{
    public double Triumph { get; set; }
    public double Success { get; set; }
    public double Failure { get; set; }
    public double Disaster { get; set; }

    public double Get(QuestGradeRole role) => role switch
    {
        QuestGradeRole.Triumph => Triumph,
        QuestGradeRole.Success => Success,
        QuestGradeRole.Failure => Failure,
        QuestGradeRole.Disaster => Disaster,
        _ => 1.0
    };
}