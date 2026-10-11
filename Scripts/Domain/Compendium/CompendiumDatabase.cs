namespace AdventurersGuild.Domain.Compendium;

public static class CompendiumDatabase
{
    public static BestiaryDatabase Bestiary { get; private set; }

    public static void Install(BestiaryDatabase bestiary = null)
    {
        if (bestiary != null) Bestiary = bestiary;
    }
}