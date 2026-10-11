using System;
using AdventurersGuild.Core;

namespace AdventurersGuild.Domain.Adventurers;

public static class AdventurerBalance
{
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
            "polynomial" => (int)(p.ExpCapLvl1 * L * (1.0 + (L - 1.0) * p.ExpCapAcceleration)),
            "quadratic" => p.ExpCapLvl1 * level * level,
            "exponential" => (int)(p.ExpCapLvl1 * Math.Pow(1.5, level - 1)),
            _ => p.ExpCapLvl1 * level,
        };
    }

    public static void SetActiveProfile(AdventurerBalanceProfile profile)
    {
        if (profile != null) active = profile;
    }
}