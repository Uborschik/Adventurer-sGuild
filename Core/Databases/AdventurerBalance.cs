using Godot;
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
                GD.PushWarning("[AdventurerBalance] Active == null, использую дефолты");
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
        GD.Print($"[AdventurerBalance] загружен: maxLevel={active.MaxLevel}, " +
                 $"expCapLvl1={active.ExpCapLvl1}, scaling={active.ExpCapScaling}");
    }

    public static int ExpCapFor(int level)
    {
        if (level >= MaxLevel) return int.MaxValue;

        var p = Active;
        int baseCap = p.ExpCapLvl1;

        if (level < 1) return baseCap;

        return p.ExpCapScaling switch
        {
            "flat" => baseCap,
            "linear" => baseCap * level,
            "quadratic" => baseCap * level * level,
            "exponential" => (int)(baseCap * Math.Pow(1.5, level - 1)),
            _ => baseCap * level,
        };
    }

    private static void Validate(AdventurerBalanceProfile p)
    {
        if (p.MaxLevel < 1)
        {
            GD.PushError($"[AdventurerBalance] maxLevel < 1, использую 100");
            p.MaxLevel = 100;
        }

        if (p.ExpCapLvl1 < 1)
        {
            GD.PushError($"[AdventurerBalance] expCapLvl1 < 1, использую 30");
            p.ExpCapLvl1 = 30;
        }

        switch (p.ExpCapScaling)
        {
            case "flat":
            case "linear":
            case "quadratic":
            case "exponential":
                break;
            default:
                GD.PushError($"[AdventurerBalance] неизвестный scaling '{p.ExpCapScaling}', использую linear");
                p.ExpCapScaling = "linear";
                break;
        }
    }
}