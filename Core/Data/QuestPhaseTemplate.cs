using System.Collections.Generic;

public class QuestPhaseTemplate
{
    public string Id { get; set; }
    public LocalizedString Name { get; set; }
    public int BaseDurationMinutes { get; set; }
    public bool Critical { get; set; }
    public int ExpReward { get; set; }
    public List<PhaseSolution> Solutions { get; set; } = new();
    public PhaseFailEffect OnFail { get; set; }
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