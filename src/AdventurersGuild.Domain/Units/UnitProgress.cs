namespace AdventurersGuild.Domain.Adventurers;

public static class UnitProgress
{
    public static void AddExperience(UnitModel model, int amount)
    {
        if (model == null || amount <= 0) return;

        var current = model.Level;
        model.SetLevel(current.SetExperience(current.Experience + amount));

        while (model.Level.CanLevelUp)
        {
            if (model.Level.Number >= LevelBalance.MaxLevel) break;
            LevelUp(model);
        }
    }

    private static void LevelUp(UnitModel model)
    {
        var current = model.Level;

        int newNumber = current.Number + 1;
        int overflow = current.Experience - current.ExpToNext;
        int newExpToNext = LevelBalance.ExpCapFor(newNumber);

        var newLevel = current.SetAfterLevelUp(newNumber, overflow, newExpToNext);
        model.SetLevel(newLevel);

        var newStats = StatGrowth.AtLevel(model.ClassId, model.RaceId, newNumber);
        model.SetStats(newStats);
    }
}