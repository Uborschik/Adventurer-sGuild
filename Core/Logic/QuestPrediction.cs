using System.Collections.Generic;

public class QuestPrediction
{
    public QuestGradeRole Role { get; set; } = QuestGradeRole.Disaster;
    public int PhasesPassed { get; set; }
    public int PhasesTotal { get; set; }
    public int CriticalPassed { get; set; }
    public int CriticalTotal { get; set; }
    public int ExpTotal { get; set; }
    public bool IsFailed { get; set; }

    // Расширение для UI
    public List<PhasePrediction> PhasePredictions { get; set; } = new();

    public float SuccessOrBetter =>
        (Role == QuestGradeRole.Success || Role == QuestGradeRole.Triumph) ? 100f : 0f;
}

public class PhasePrediction
{
    public string PhaseId;
    public bool ExpectedPass;
    public double MinChance;
    public List<SkillRollResult> Rolls;
}