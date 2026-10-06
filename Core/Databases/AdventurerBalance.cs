using System;

public static class AdventurerBalance
{
    private const string Path = "res://Resources/Data/Adventurer/AdventurerBalance.json";

    private static AdventurerBalanceProfile active;

    public static AdventurerBalanceProfile Active
    {
        get
        {
            if (active == null)
            {
                Log.Warn("[AdventurerBalance] Active == null, использую дефолты");
                active = new AdventurerBalanceProfile();
            }
            return active;
        }
    }

    public static int MaxLevel => Active.MaxLevel;

    public static void Load()
    {
        active = JsonLoader.Load<AdventurerBalanceProfile>(Path);
        Validate(active);
        Log.Info($"[AdventurerBalance] загружен: maxLevel={active.MaxLevel}, " +
                 $"expCapLvl1={active.ExpCapLvl1}, scaling={active.ExpCapScaling}, " +
                 $"acceleration={active.ExpCapAcceleration}");
    }

    public static int ExpCapFor(int level)
    {
        if (level >= MaxLevel) return int.MaxValue;
        if (level < 1) return Active.ExpCapLvl1;

        var p = Active;
        double L = level;

        return p.ExpCapScaling switch
        {
            "flat" => p.ExpCapLvl1,
            "linear" => p.ExpCapLvl1 * level,
            "polynomial" => (int)(p.ExpCapLvl1 * L
                                  * (1.0 + (L - 1.0) * p.ExpCapAcceleration)),
            "quadratic" => p.ExpCapLvl1 * level * level,
            "exponential" => (int)(p.ExpCapLvl1 * Math.Pow(1.5, level - 1)),
            _ => p.ExpCapLvl1 * level,
        };
    }

    private static void Validate(AdventurerBalanceProfile p)
    {
        if (p.MaxLevel < 1)
        {
            Log.Error("[AdventurerBalance] maxLevel < 1, использую 60");
            p.MaxLevel = 60;
        }

        if (p.ExpCapLvl1 < 1)
        {
            Log.Error("[AdventurerBalance] expCapLvl1 < 1, использую 45");
            p.ExpCapLvl1 = 45;
        }

        switch (p.ExpCapScaling)
        {
            case "flat":
            case "linear":
            case "polynomial":
            case "quadratic":
            case "exponential":
                break;
            default:
                Log.Error($"[AdventurerBalance] неизвестный scaling '{p.ExpCapScaling}', использую polynomial");
                p.ExpCapScaling = "polynomial";
                break;
        }

        if (p.ExpCapScaling == "polynomial" && p.ExpCapAcceleration <= 0)
        {
            Log.Warn("[AdventurerBalance] acceleration <= 0, использую 0.22");
            p.ExpCapAcceleration = 0.22;
        }

        if (p.StatBaseValue < 0)
        {
            Log.Error("[AdventurerBalance] statBaseValue < 0, использую 0");
            p.StatBaseValue = 0.0;
        }

        if (p.StatStartPool <= 0)
        {
            Log.Error("[AdventurerBalance] statStartPool <= 0, использую 16");
            p.StatStartPool = 16.0;
        }

        if (p.StatBaseSlope <= 0)
        {
            Log.Error("[AdventurerBalance] statBaseSlope <= 0, использую 2.5");
            p.StatBaseSlope = 2.5;
        }
    }
}