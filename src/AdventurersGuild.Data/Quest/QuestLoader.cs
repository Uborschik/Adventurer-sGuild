namespace AdventurersGuild.Data.Quests;

using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Core;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Domain.Quests;

public static class QuestLoader
{
    private const string questPath = "Data/Quests/";

    public static void Load()
    {
        var roleDb = JsonLoader.Load<RoleDatabase>(questPath + "Roles.json");

        var roles = new Dictionary<string, RoleInfo>();
        foreach (var r in roleDb.Combat) roles[r.Id] = r with { Type = EncounterType.Combat };
        foreach (var r in roleDb.Social) roles[r.Id] = r with { Type = EncounterType.Social };

        var instructions = JsonLoader.Load<InstructionDatabase>(questPath + "Instructions.json")
            .Instructions.ToDictionary(i => i.Id);

        var phases = JsonLoader.Load<PhaseDatabase>(questPath + "Phases.json")
            .Phases.ToDictionary(p => p.Id);

        var habitats = JsonLoader.Load<BestiaryDatabase>(questPath + "Bestiary.json")
            .Habitats.ToDictionary(h => h.LocationId);

        var templates = JsonLoader.Load<QuestTemplateDatabase>(questPath + "QuestTemplates.json")
            .Templates.ToDictionary(t => t.Id);

        var locations = JsonLoader.Load<LocationDatabase>(questPath + "Locations.json")
            .Locations.ToDictionary(l => l.Id);

        var balanceDb = JsonLoader.Load<QuestBalanceDatabase>(questPath + "QuestBalance.json");

        if (balanceDb?.Profiles != null && balanceDb.Profiles.Count > 0)
        {
            var defaultProfile = balanceDb.Profiles.FirstOrDefault(p => p.Id == "default")
                                 ?? balanceDb.Profiles.First();

            var balanceDict = balanceDb.Profiles.ToDictionary(p => p.Id);

            QuestBalance.Install(balanceDict, defaultProfile);
        }
        else
        {
            Log.Error("[QuestLoader] QuestBalance.json пуст или не загружен!");
        }

        var grades = JsonLoader.Load<QuestGradeDatabase>(questPath + "QuestGrades.json")
            .Grades.ToDictionary(g => g.Role);

        QuestDatabase.Install(roles, instructions, phases, habitats, templates, locations, grades);
    }
}