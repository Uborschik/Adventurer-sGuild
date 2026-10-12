namespace AdventurersGuild.Domain.Quests;

public struct SkillRollResult
{
    public string SkillId;
    public string BestAdvName;
    public double SkillValue;
    public double Required;
    public double Ratio;
    public double Chance;
    public int Threshold;
    public int Roll;
    public int RollMax;
    public bool Passed;
    public int SolutionIndex;
}