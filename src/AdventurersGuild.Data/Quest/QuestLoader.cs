namespace AdventurersGuild.Data.Quest;

using System.Linq;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Domain.Quests;

public static class QuestLoader
{
    private const string unitsPath = "Data/Units/";
    private const string questPath = "Data/Quest/";

    public static void Load()
    {
        var roles = JsonLoader.Load<RoleDatabase>(unitsPath + "Roles.json")
            .Roles.ToDictionary(r => r.Id);

        var instructions = JsonLoader.Load<InstructionDatabase>(unitsPath + "Instructions.json")
            .Instructions.ToDictionary(i => i.Id);

        var phases = JsonLoader.Load<PhaseDatabase>(unitsPath + "Phases.json")
            .Phases.ToDictionary(p => p.Id);

        var habitats = JsonLoader.Load<BestiaryDatabase>(questPath + "Bestiary.json")
            .Habitats.ToDictionary(h => h.LocationId);

        var templates = JsonLoader.Load<QuestTemplateDatabase>(questPath + "QuestTemplates.json")
            .Templates.ToDictionary(t => t.Id);

        var locations = JsonLoader.Load<LocationDatabase>(questPath + "Locations.json")
            .Locations.ToDictionary(l => l.Id);

        QuestDatabase.Install(roles, instructions, phases, habitats, templates, locations);

        QuestValidator.Validate();
    }
}