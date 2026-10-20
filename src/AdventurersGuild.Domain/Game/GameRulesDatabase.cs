namespace AdventurersGuild.Domain.Game;

using System.Collections.Generic;

public class GameRulesDatabase
{
    public int InitialAdventurerLvl { get; set; } = 1;
    public int InitialQuestLvl { get; set; } = 1;
    public List<string> AdventurerSpecies { get; set; } = [];
}