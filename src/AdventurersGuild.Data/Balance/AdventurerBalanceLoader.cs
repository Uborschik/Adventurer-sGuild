using AdventurersGuild.Core;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Domain.Adventurers;

namespace AdventurersGuild.Data.Balance;

public static class AdventurerBalanceLoader
{
    private const string Path = "Resources/Data/Adventurer/AdventurerBalance.json";

    public static void Load()
    {
        var profile = JsonLoader.Load<AdventurerBalanceProfile>(Path);
        Validate(profile);
        AdventurerBalance.SetActiveProfile(profile);
        Log.Info($"[AdventurerBalance] загружен: maxLevel={profile.MaxLevel}, " +
                 $"expCapLvl1={profile.ExpCapLvl1}, scaling={profile.ExpCapScaling}, " +
                 $"acceleration={profile.ExpCapAcceleration}");
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