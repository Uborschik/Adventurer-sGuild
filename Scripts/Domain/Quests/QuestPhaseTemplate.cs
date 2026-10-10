using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

namespace AdventurersGuild.Domain.Quests;

public class QuestPhaseTemplate
{
    public string Id { get; set; }
    public LocalizedString Name { get; set; }

    public bool IsMarker { get; set; }

    public List<PhaseSolution> Solutions { get; set; } = new();

    public List<PhasePath> Paths { get; set; } = new();

    public bool Critical { get; set; }
    public int BaseDurationMinutes { get; set; }
    public int ExpReward { get; set; }
    public PhaseFailEffect OnFail { get; set; }
}

public class PhasePath
{
    public List<PhaseCheck> Checks { get; set; } = new();
}

public class PhaseCheck
{
    public string Skill { get; set; }
    public List<string> Resistance { get; set; } = new();
}

public class PhaseSolution
{
    public Dictionary<string, double> Skills { get; set; } = new();
}

public class PhaseFailEffect
{
    public string TargetPhase { get; set; }
    public double DcDelta { get; set; }
    public int DurationDeltaMinutes { get; set; }
}

public class QuestPhaseDatabase
{
    public List<QuestPhaseTemplate> Phases { get; set; } = new();
}