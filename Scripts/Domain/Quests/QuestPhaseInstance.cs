namespace AdventurersGuild.Domain.Quests;

using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

public class QuestPhaseInstance
{
    public string RoleId { get; set; }
    public string PhaseId { get; set; }
    public LocalizedString Name { get; set; }
    public List<PhaseSolution> Solutions { get; set; } = new();
    public int DurationMinutes { get; set; }
    public int ExpReward { get; set; }
    public bool Critical { get; set; }
    public QuestTransition? NextOnPass { get; set; }
    public QuestTransition? NextOnFail { get; set; }
}

public readonly record struct QuestTransition(string ToRoleId, double DcDelta, int DurationDelta);