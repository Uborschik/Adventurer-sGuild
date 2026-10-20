namespace AdventurersGuild.Domain.Units;

using System;
using AdventurersGuild.Core;

public static class LevelBalance
{
    private static LevelBalanceProfile active;

    public static LevelBalanceProfile Active
    {
        get
        {
            if (active == null)
            {
                Log.Warn("[LevelBalance] Active == null, использую дефолты");
                active = new LevelBalanceProfile();
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

    public static void SetActiveProfile(LevelBalanceProfile profile)
    {
        if (profile != null) active = profile;
    }
}