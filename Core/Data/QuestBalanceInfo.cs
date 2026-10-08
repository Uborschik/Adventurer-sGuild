using System.Collections.Generic;

public class QuestBalanceProfile
{
    public string Id { get; set; }

    public TierMultiplierMap DurationTierMultiplier { get; set; } = new()
    {
        Easy = 1.0,
        Normal = 1.2,
        Hard = 1.5
    };
    public double DurationFailPenaltyPerPhase { get; set; } = 0.30;
    public double DurationIntBonusCap { get; set; } = 0.30;
    public double DurationIntBonusK { get; set; } = 0.15;

    public SkillRollBalance SkillRoll { get; set; } = new();

    public double EscapeStatK { get; set; } = 40;
    public double EscapeStatCap { get; set; } = 95;
    public EscapeBaseByTier EscapeBaseByTier { get; set; } = new();
    public double EscapeEndWeight { get; set; } = 0.8;
    public double EscapeWisWeight { get; set; } = 0.5;

    public WoundDaysByTier WoundFailDaysByTier { get; set; } = new();
    public WoundSuccessByTier WoundSuccessByTier { get; set; } = new();

    public double NightAttackChanceBase { get; set; } = 25;
    public int MaxQuestDays { get; set; } = 90;
    public int NightCreatureLevelPenalty { get; set; } = 5;

    public double EnduranceInjuryK { get; set; } = 60;
    public double EnduranceInjuryCap { get; set; } = 0.75;
    public double EnduranceDeathK { get; set; } = 150;
    public double EnduranceDeathCap { get; set; } = 0.50;

    public ExperienceBalance Experience { get; set; } = new();
}

public class QuestBalanceDatabase
{
    public List<QuestBalanceProfile> Profiles { get; set; } = new();
}

public class ExperienceBalance
{
    public double ContributionBaseScore { get; set; } = 0.2;
}

public class SkillRollBalance
{
    public double Base { get; set; } = 50;
    public double PerRatio { get; set; } = 50;
    public double Min { get; set; } = 10;
    public double Max { get; set; } = 95;
}

// === Тир-множители длительности ===

public class TierMultiplierMap
{
    public double Easy { get; set; }
    public double Normal { get; set; }
    public double Hard { get; set; }

    public double Get(QuestTier tier) => tier switch
    {
        QuestTier.Easy => Easy,
        QuestTier.Normal => Normal,
        QuestTier.Hard => Hard,
        _ => Normal
    };
}

// === Escape база ===

public class EscapeBaseByTier
{
    public double Easy { get; set; } = 60;
    public double Normal { get; set; } = 45;
    public double Hard { get; set; } = 30;

    public double Get(string tier) => tier?.ToLowerInvariant() switch
    {
        "easy" => Easy,
        "normal" => Normal,
        "hard" => Hard,
        _ => Normal
    };
}

// === Раны при провале ===

public class WoundDaysByTier
{
    public WoundRange Easy { get; set; } = new() { Min = 2, Max = 4 };
    public WoundRange Normal { get; set; } = new() { Min = 3, Max = 6 };
    public WoundRange Hard { get; set; } = new() { Min = 5, Max = 10 };

    public WoundRange Get(string tier) => tier?.ToLowerInvariant() switch
    {
        "easy" => Easy,
        "normal" => Normal,
        "hard" => Hard,
        _ => Normal
    };
}

public class WoundRange
{
    public int Min { get; set; }
    public int Max { get; set; }
}

// === Раны при успехе ===

public class WoundSuccessByTier
{
    public WoundSuccessRange Easy { get; set; } = new() { Chance = 5, Min = 1, Max = 2 };
    public WoundSuccessRange Normal { get; set; } = new() { Chance = 15, Min = 1, Max = 3 };
    public WoundSuccessRange Hard { get; set; } = new() { Chance = 30, Min = 2, Max = 4 };

    public WoundSuccessRange Get(string tier) => tier?.ToLowerInvariant() switch
    {
        "easy" => Easy,
        "normal" => Normal,
        "hard" => Hard,
        _ => Normal
    };
}

public class WoundSuccessRange
{
    public int Chance { get; set; }
    public int Min { get; set; }
    public int Max { get; set; }
}