#nullable enable

namespace AdventurersGuild.Editor.Compendium.Schemas;

using AdventurersGuild.Editor.Compendium.Schema;

public static class PhaseSchema
{
    public static EntitySchema Build() => new()
    {
        Kind = "phase",
        SingularTitle = "Фаза",
        PluralTitle = "Фазы",
        Section = "Мир/Фазы",
        Order = 15,
        File = "Data/Units/Phases.json",
        RootPath = "phases",
        DisplayField = "name",

        Fields =
        [
            new FieldSchema { Name = "id", Label = "ID", Type = CompendiumFieldType.Id, Required = true, Group = "Основное", Order = 1, Width = 6 },
            new FieldSchema { Name = "name", Label = "Название", Type = CompendiumFieldType.Localized, Required = true, Group = "Основное", Order = 2, Width = 12 },
            new FieldSchema { Name = "description", Label = "Описание", Type = CompendiumFieldType.Localized, Required = false, Group = "Основное", Order = 3, Width = 12 },
        ],
    };
}