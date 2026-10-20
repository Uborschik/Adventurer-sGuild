namespace AdventurersGuild.Data.Rules;

using AdventurersGuild.Core;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Domain.Game;

public static class GameRulesLoader
{
    private const string dataPath = "Data/Config/";

    public static void Load()
    {
        var rules = JsonLoader.Load<GameRulesDatabase>(dataPath + "GameRules.json");

        if (rules == null)
        {
            Log.Error("[GameLoader] Не удалось загрузить GameRules.json");
            return;
        }

        GameRules.Install(rules);

        Log.Info($"[GameLoader] Правила загружены: стартовый Lvl авантюриста = {GameRules.InitialAdventurerLvl}");
    }
}