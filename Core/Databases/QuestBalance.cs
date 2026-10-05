using System.Collections.Generic;
using System.Linq;

public static class QuestBalance
{
    private const string Path = "res://Resources/Data/Quest/QuestBalance.json";

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

    public static void Load(string activeId = "default")
    {
        var db = JsonLoader.Load<QuestBalanceDatabase>(Path);
        profiles = db.Profiles.ToDictionary(p => p.Id);

        if (!profiles.TryGetValue(activeId, out var profile))
        {
            Log.Error($"[QuestBalance] Нет профиля '{activeId}', беру первый");
            profile = db.Profiles.FirstOrDefault();
        }

        if (profile == null)
        {
            Log.Error("[QuestBalance] Нет ни одного профиля — дефолты");
            profile = new QuestBalanceProfile { Id = "fallback" };
        }

        Validate(profile);
        active = profile;
        Log.Info($"[QuestBalance] профиль '{Active.Id}' загружен");
    }

    public static QuestBalanceProfile Get(string id)
        => profiles != null && profiles.TryGetValue(id, out var p) ? p : null;

    private static void Validate(QuestBalanceProfile p)
    {
        if (p.ReferenceStatBase <= 0)
            Log.Error($"[QuestBalance] '{p.Id}': referenceStatBase <= 0");

        if (p.ReferenceStatSlope <= 0)
            Log.Error($"[QuestBalance] '{p.Id}': referenceStatSlope <= 0");

        if (p.CoverageThreshold < 0 || p.CoverageThreshold > 2)
            Log.Error($"[QuestBalance] '{p.Id}': coverageThreshold вне [0, 2]");

        if (p.MarginTriumph < p.MarginSuccess)
            Log.Error($"[QuestBalance] '{p.Id}': marginTriumph < marginSuccess");

        if (p.MarginSuccess < p.MarginFailure)
            Log.Error($"[QuestBalance] '{p.Id}': marginSuccess < marginFailure");

        if (p.TriumphCapPercent < 0 || p.TriumphCapPercent > 100)
            Log.Error($"[QuestBalance] '{p.Id}': triumphCapPercent вне [0, 100]");

        if (p.EnduranceInjuryK <= 0)
            Log.Error($"[QuestBalance] '{p.Id}': enduranceInjuryK <= 0");
        if (p.EnduranceInjuryCap < 0 || p.EnduranceInjuryCap > 1)
            Log.Error($"[QuestBalance] '{p.Id}': enduranceInjuryCap вне [0, 1]");

        if (p.EnduranceDeathK <= 0)
            Log.Error($"[QuestBalance] '{p.Id}': enduranceDeathK <= 0");
        if (p.EnduranceDeathCap < 0 || p.EnduranceDeathCap > 1)
            Log.Error($"[QuestBalance] '{p.Id}': enduranceDeathCap вне [0, 1]");

        if (p.TierBonus.Easy > p.TierBonus.Normal || p.TierBonus.Normal > p.TierBonus.Hard)
            Log.Error($"[QuestBalance] '{p.Id}': tierBonus не возрастает Easy→Normal→Hard");

        if (p.Experience == null)
        {
            Log.Error($"[QuestBalance] '{p.Id}': нет блока experience");
        }
        else
        {
            if (p.Experience.ExpBonusLvl1 < 0)
                Log.Error($"[QuestBalance] '{p.Id}': expBonusLvl1 < 0");

            if (p.Experience.ContributionBaseScore < 0)
                Log.Error($"[QuestBalance] '{p.Id}': contributionBaseScore < 0");
        }
    }
}