using System.Collections.Generic;
using Godot;

public static class StatGrowth
{
    public const int Cap = 100;
    public const int BaseValue = 4;
    public const int ClassBonus = 2;
    private const float SecondaryGrowth = 0.7f;

    public static Dictionary<string, int> AtLevel(string classId, string raceId, int level = 1)
    {
        var cls = AdventurerDatabase.GetClass(classId);
        var race = AdventurerDatabase.GetRace(raceId);

        if (cls == null || race == null)
            return new Dictionary<string, int>();

        int levelsAboveOne = Mathf.Max(0, level - 1);

        int primaryValue = BaseValue + ClassBonus + levelsAboveOne;
        int secondaryValue = (int)(BaseValue + levelsAboveOne * SecondaryGrowth);

        var result = new Dictionary<string, int>();

        foreach (var statId in AdventurerDatabase.Stats.Keys)
        {
            int value = (statId == cls.PrimaryStat) ? primaryValue : secondaryValue;

            if (race.StatBonuses.TryGetValue(statId, out int bonus))
                value += bonus;

            result[statId] = Mathf.Min(Cap, value);
        }

        return result;
    }
}