namespace AdventurersGuild.Data.Compendium;

using AdventurersGuild.Data.IO;
using AdventurersGuild.Domain.Compendium;

public static class CompendiumLoader
{
    private const string dataPath = "Data/Compendium/";

    public static void Load()
    {
        var bestiary = JsonLoader.Load<BestiaryDatabase>(dataPath + "Bestiary.json");
        CompendiumDatabase.Install(bestiary);

        // CompendiumValidator.Validate();
    }
}