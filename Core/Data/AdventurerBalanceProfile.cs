public class AdventurerBalanceProfile
{
    public int MaxLevel { get; set; } = 60;
    public int ExpCapLvl1 { get; set; } = 45;
    public string ExpCapScaling { get; set; } = "polynomial";
    public double ExpCapAcceleration { get; set; } = 0.22;

    public double StatBaseValue { get; set; } = 0.0;
    public double StatStartPool { get; set; } = 10.0;
    public double StatBaseSlope { get; set; } = 2.5;
}