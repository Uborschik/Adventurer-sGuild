using System.Collections.Generic;
using AdventurersGuild.Core;

namespace AdventurersGuild.Domain.Quests;

public static class QuestBalance
{
    private static Dictionary<string, QuestBalanceProfile> profiles;
    private static QuestBalanceProfile active;

    public static QuestBalanceProfile Active
    {
        get
        {
            if (active == null)
            {
                Log.Warn("[QuestBalance] Active == null, использую дефолты");
                active = new QuestBalanceProfile { Id = "fallback" };
            }
            return active;
        }
    }

    public static void Install(Dictionary<string, QuestBalanceProfile> all, QuestBalanceProfile current)
    {
        profiles = all;
        active = current;
    }

    public static QuestBalanceProfile Get(string id)
        => profiles != null && profiles.TryGetValue(id, out var p) ? p : null;

    public static void SetActiveProfile(QuestBalanceProfile profile)
    {
        if (profile != null) active = profile;
    }
}