using System.Collections.Generic;

namespace AdventurersGuild.Domain.Quests;

public class QuestBalanceProfile
{
    public string Id { get; set; }

    // === Duration ===
    public double DurationMultiplier { get; set; } = 1.2;
    public double DurationFailPenaltyPerPhase { get; set; } = 0.30;
    public double DurationIntBonusCap { get; set; } = 0.30;
    public double DurationIntBonusK { get; set; } = 0.15;

    // === Skill roll ===
    public SkillRollBalance SkillRoll { get; set; } = new();

    // === Escape ===
    public double EscapeStatK { get; set; } = 40;
    public double EscapeStatCap { get; set; } = 95;
    public double EscapeBase { get; set; } = 45;
    public double EscapeEndWeight { get; set; } = 0.8;
    public double EscapeWisWeight { get; set; } = 0.5;

    // === Wounds ===
    public WoundRange WoundFailDays { get; set; } = new() { Min = 3, Max = 6 };
    public WoundSuccessRange WoundSuccess { get; set; } = new() { Chance = 15, Min = 1, Max = 3 };

    // === Night & limits ===
    public double NightAttackChanceBase { get; set; } = 25;
    public int MaxQuestDays { get; set; } = 90;
    public int NightCreatureLevelPenalty { get; set; } = 5;

    // === Endurance ===
    public double EnduranceInjuryK { get; set; } = 60;
    public double EnduranceInjuryCap { get; set; } = 0.75;
    public double EnduranceDeathK { get; set; } = 150;
    public double EnduranceDeathCap { get; set; } = 0.50;

    // === Experience ===
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
    public double Min { get; set; } = 5;
    public double Max { get; set; } = 80;
}

// === Раны ===

public class WoundRange
{
    public int Min { get; set; }
    public int Max { get; set; }
}

public class WoundSuccessRange
{
    public int Chance { get; set; }
    public int Min { get; set; }
    public int Max { get; set; }
}