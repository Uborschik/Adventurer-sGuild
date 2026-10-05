using System.Collections.Generic;
using System.Linq;

public static class XpStats
{
    private struct Row
    {
        public int Quests;
        public int TotalXp;
        public int MinXp;
        public int MaxXp;
    }

    private static readonly Dictionary<int, Row> byQuestLevel = new();
    private static int totalQuests;
    private static int totalXp;
    private static int printEveryNQuests = 20;

    public static void Record(int questLevel, int xpGiven)
    {
        if (!byQuestLevel.TryGetValue(questLevel, out var row))
            row = new Row { MinXp = int.MaxValue, MaxXp = 0 };

        row.Quests++;
        row.TotalXp += xpGiven;
        if (xpGiven < row.MinXp) row.MinXp = xpGiven;
        if (xpGiven > row.MaxXp) row.MaxXp = xpGiven;
        byQuestLevel[questLevel] = row;

        totalQuests++;
        totalXp += xpGiven;

        if (printEveryNQuests > 0 && totalQuests % printEveryNQuests == 0)
            Print();
    }

    public static void Print()
    {
        if (totalQuests == 0)
        {
            Log.Warn("[XpStats] Пока нет завершённых квестов.");
            return;
        }

        Log.Info($"[XpStats] Всего квестов: {totalQuests}, суммарный XP: {totalXp}");

        foreach (var kv in byQuestLevel.OrderBy(k => k.Key))
        {
            int level = kv.Key;
            var r = kv.Value;
            double avg = r.Quests > 0 ? (double)r.TotalXp / r.Quests : 0;
            Log.Info($"  L{level:D2}: {r.Quests,4} кв. | avg {avg,7:F1} | " +
                     $"min {r.MinXp,4} | max {r.MaxXp,4} | total {r.TotalXp}");
        }
    }

    public static void Reset()
    {
        byQuestLevel.Clear();
        totalQuests = 0;
        totalXp = 0;
    }

    public static void SetAutoprintEvery(int n) => printEveryNQuests = n;
}