using AdventurersGuild.Core;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Domain.Adventurers;

namespace AdventurersGuild.Data.Balance;

public static class LevelBalanceLoader
{
    private const string path = "Data/Units/LevelBalance.json";

    public static void Load()
    {
        var profile = JsonLoader.Load<LevelBalanceProfile>(path);
        Validate(profile);
        LevelBalance.SetActiveProfile(profile);
        Log.Info($"[LevelBalance] загружен: maxLevel={profile.MaxLevel}, " +
                 $"expCapLvl1={profile.ExpCapLvl1}, scaling={profile.ExpCapScaling}, " +
                 $"acceleration={profile.ExpCapAcceleration}");
    }

    private static void Validate(LevelBalanceProfile p)
    {
        if (p.MaxLevel < 1)
        {
            Log.Error("[LevelBalance] maxLevel < 1, использую 60");
            p.MaxLevel = 60;
        }

        if (p.ExpCapLvl1 < 1)
        {
            Log.Error("[LevelBalance] expCapLvl1 < 1, использую 45");
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
                Log.Error($"[LevelBalance] неизвестный scaling '{p.ExpCapScaling}', использую polynomial");
                p.ExpCapScaling = "polynomial";
                break;
        }

        if (p.ExpCapScaling == "polynomial" && p.ExpCapAcceleration <= 0)
        {
            Log.Warn("[LevelBalance] acceleration <= 0, использую 0.22");
            p.ExpCapAcceleration = 0.22;
        }

        if (p.StatBaseValue < 0)
        {
            Log.Error("[LevelBalance] statBaseValue < 0, использую 0");
            p.StatBaseValue = 0.0;
        }

        if (p.StatStartPool <= 0)
        {
            Log.Error("[LevelBalance] statStartPool <= 0, использую 16");
            p.StatStartPool = 16.0;
        }

        if (p.StatBaseSlope <= 0)
        {
            Log.Error("[LevelBalance] statBaseSlope <= 0, использую 2.5");
            p.StatBaseSlope = 2.5;
        }
    }
}