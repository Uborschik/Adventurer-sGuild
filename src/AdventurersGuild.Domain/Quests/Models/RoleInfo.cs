namespace AdventurersGuild.Domain.Quests;

using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

public record RoleInfo
{
    public string Id { get; init; } = string.Empty;
    public EncounterType Type { get; init; }
    public LocalizedString Name { get; init; }
    public List<ActionInfo> Actions { get; init; } = [];
}

public record ActionInfo
{
    public string Id { get; init; } = string.Empty;
    public LocalizedString Name { get; init; }
    public string PhaseId { get; init; } = string.Empty;
    public int Duration { get; init; }
    public CheckInfo? Check { get; init; }
    public OutcomeInfo Success { get; init; } = new();
    public OutcomeInfo Fail { get; init; } = new();
}

public readonly record struct CheckInfo(
    string ActorStat,
    string TargetSkill,
    string TargetStat
);

public record OutcomeInfo
{
    public string Id { get; init; } = string.Empty;
    public int Actor { get; init; }
    public int Target { get; init; }
    public List<ConsequenceInfo> Consequences { get; init; } = [];
}

public readonly record struct ConsequenceInfo(
    string Id,
    int Chance,
    int IfWounded,
    int DelayMinutes,
    double DcDelta
);

public record RoleDatabase
{
    public List<RoleInfo> Combat { get; init; } = [];
    public List<RoleInfo> Social { get; init; } = [];
}