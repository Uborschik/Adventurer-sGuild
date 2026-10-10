namespace AdventurersGuild.Data.Compendium;

using AdventurersGuild.Core;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Domain.Compendium;

public static partial class CompendiumDatabase
{
    private const string DataPath = "res://Resources/Data/Compendium/";

    public static void Load()
    {
        LoadBestiary();

        Validate();
    }

    private static void LoadBestiary()
    {
        Bestiary = JsonLoader.Load<BestiaryDatabase>(DataPath + "Bestiary.json");
    }
}