using Godot;
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
                GD.PushWarning("[QuestBalance] Active == null, использую дефолты");
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
            GD.PushError($"[QuestBalance] Нет профиля '{activeId}', беру первый");
            profile = db.Profiles.FirstOrDefault();
        }

        if (profile == null)
        {
            GD.PushError("[QuestBalance] Нет ни одного профиля — дефолты");
            profile = new QuestBalanceProfile { Id = "fallback" };
        }

        Validate(profile);
        active = profile;
        GD.Print($"[QuestBalance] профиль '{Active.Id}' загружен");
    }

    public static QuestBalanceProfile Get(string id)
        => profiles != null && profiles.TryGetValue(id, out var p) ? p : null;

    private static void Validate(QuestBalanceProfile p)
    {
        if (p.ReferenceStatAtLevel1 <= 0)
            GD.PushError($"[QuestBalance] '{p.Id}': referenceStatAtLevel1 <= 0");

        if (p.CoverageThreshold < 0 || p.CoverageThreshold > 2)
            GD.PushError($"[QuestBalance] '{p.Id}': coverageThreshold вне [0, 2]");

        if (p.MarginTriumph < p.MarginSuccess)
            GD.PushError($"[QuestBalance] '{p.Id}': marginTriumph < marginSuccess");

        if (p.MarginSuccess < p.MarginFailure)
            GD.PushError($"[QuestBalance] '{p.Id}': marginSuccess < marginFailure");

        if (p.TriumphCapPercent < 0 || p.TriumphCapPercent > 100)
            GD.PushError($"[QuestBalance] '{p.Id}': triumphCapPercent вне [0, 100]");

        if (p.TierBonus.Easy > p.TierBonus.Normal || p.TierBonus.Normal > p.TierBonus.Hard)
            GD.PushError($"[QuestBalance] '{p.Id}': tierBonus не возрастает Easy→Normal→Hard");

        if (p.Experience == null)
            GD.PushError($"[QuestBalance] '{p.Id}': нет блока experience");
        else
        {
            if (p.Experience.ExpBonusLvl1 < 0)
                GD.PushError($"[QuestBalance] '{p.Id}': expBonusLvl1 < 0");

            if (p.Experience.ContributionBaseScore < 0)
                GD.PushError($"[QuestBalance] '{p.Id}': contributionBaseScore < 0");
        }
    }
}