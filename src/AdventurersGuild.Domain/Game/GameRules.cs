namespace AdventurersGuild.Domain.Game;

using System.Collections.Generic;

public static class GameRules
{
    public static int InitialAdventurerLvl { get; private set; } = 1;
    public static int InitialQuestLvl { get; private set; } = 1;
    public static List<string> AdventurerSpecies { get; private set; } = new();

    public static void Install(GameRulesDatabase db)
    {
        if (db == null) return;

        InitialAdventurerLvl = db.InitialAdventurerLvl;
        InitialQuestLvl = db.InitialQuestLvl;
        AdventurerSpecies = db.AdventurerSpecies;
    }
}