namespace AdventurersGuild.Domain.Combat;

using System;
using AdventurersGuild.Domain.Quests;
using AdventurersGuild.Domain.Units;

public static class CombatMath
{
    private static readonly Random Rng = Random.Shared;
    private static CombatBalance Rules => LevelBalance.Active.Combat;

    public static int RollDice() => Rng.Next(1, Rules.DiceSides + 1);

    public static int CalculateAc(double dex, double end)
        => Rules.AcBase + (int)(dex * Rules.AcDexMod) + (int)(end * Rules.AcEndMod);

    public static int CalculateDamage(double attackValue)
        => (int)(attackValue * Rules.DamageMultiplier);

    public static WoundLevel GetWoundLevel(int currentHp, int maxHp)
    {
        if (currentHp <= 0) return WoundLevel.Dead;
        if (currentHp <= maxHp * Rules.WoundHeavyThreshold) return WoundLevel.Heavy;
        if (currentHp <= maxHp * Rules.WoundMediumThreshold) return WoundLevel.Medium;
        if (currentHp < maxHp) return WoundLevel.Light;
        return WoundLevel.None;
    }
}