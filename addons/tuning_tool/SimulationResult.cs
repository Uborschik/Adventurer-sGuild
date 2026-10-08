public class SimulationResult
{
    public int Trials { get; set; }

    public int TriumphCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int DisasterCount { get; set; }

    public double AvgPhasesPassed { get; set; }
    public double AvgExpEarned { get; set; }

    public float TriumphChance => Trials > 0 ? 100f * TriumphCount / Trials : 0f;
    public float SuccessChance => Trials > 0 ? 100f * SuccessCount / Trials : 0f;
    public float FailureChance => Trials > 0 ? 100f * FailureCount / Trials : 0f;
    public float DisasterChance => Trials > 0 ? 100f * DisasterCount / Trials : 0f;

    public float SuccessOrBetterChance => TriumphChance + SuccessChance;
}