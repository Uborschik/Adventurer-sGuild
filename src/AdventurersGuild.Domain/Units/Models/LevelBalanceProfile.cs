namespace AdventurersGuild.Domain.Units;

public class LevelBalanceProfile
{
    public int MaxLevel { get; set; } = 60;
    public int ExpCapLvl1 { get; set; } = 45;
    public string ExpCapScaling { get; set; } = "polynomial";
    public double ExpCapAcceleration { get; set; } = 0.22;

    public double StatBaseValue { get; set; } = 0.0;
    public double StatStartPool { get; set; } = 10.0;
    public double StatBaseSlope { get; set; } = 3.5;

    public int HpPerEndurance { get; set; } = 5;

    public CombatBalance Combat { get; set; } = new();
}

public class CombatBalance
{
    public int MaxRounds { get; set; } = 15;
    public int DiceSides { get; set; } = 20;
    public double DamageMultiplier { get; set; } = 1.5;

    public int AcBase { get; set; } = 10;
    public double AcDexMod { get; set; } = 0.5;
    public double AcEndMod { get; set; } = 0.5;

    public int DefaultStatFallback { get; set; } = 10;
    public double DefaultGrowthWeight { get; set; } = 0.2;

    public double WoundHeavyThreshold { get; set; } = 0.3;
    public double WoundMediumThreshold { get; set; } = 0.6;
}