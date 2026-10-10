namespace AdventurersGuild.Data.Compendium;

using AdventurersGuild.Domain.Compendium;

public static partial class CompendiumDatabase
{
    public static BestiaryDatabase Bestiary { get; private set; }

    public static void ApplyData(BestiaryDatabase bestiary = null)
    {
        if (bestiary != null) Bestiary = bestiary;
    }
}