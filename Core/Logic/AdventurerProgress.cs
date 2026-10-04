public static class AdventurerProgress
{
    public static void AddExperience(AdventurerModel model, int amount)
    {
        if (model == null || amount <= 0) return;

        var current = model.Level;
        model.SetLevel(current.SetExperience(current.Experience + amount));

        while (model.Level.CanLevelUp)
        {
            if (model.Level.Number >= AdventurerBalance.MaxLevel) break;
            LevelUp(model);
        }
    }

    private static void LevelUp(AdventurerModel model)
    {
        var current = model.Level;

        int newNumber = current.Number + 1;
        int overflow = current.Experience - current.ExpToNext;
        int newExpToNext = AdventurerBalance.ExpCapFor(newNumber);

        var newLevel = current.SetAfterLevelUp(newNumber, overflow, newExpToNext);
        model.SetLevel(newLevel);

        var newStats = StatGrowth.AtLevel(model.ClassId, model.RaceId, newNumber);
        model.SetStats(newStats);
    }
}