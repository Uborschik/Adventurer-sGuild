#nullable enable

namespace AdventurersGuild.Editor.Compendium.Schemas;

using AdventurersGuild.Editor.Compendium.Schema;

public static class TestSchema
{
    public static EntitySchema Build() => new()
    {
        Kind = "debug-test",
        SingularTitle = "Тест",
        PluralTitle = "Тесты",
        Section = "Прочее",
        Order = 99,
        File = "Data/Debug/TestDictionary.json",
        RootPath = "items",
        DisplayField = "name",

        Fields =
        [
            new FieldSchema { Name = "id", Label = "ID", Type = CompendiumFieldType.Id, Required = true },
            new FieldSchema { Name = "name", Label = "Название", Type = CompendiumFieldType.Localized, Required = true },
            new FieldSchema
            {
                Name = "weights",
                Label = "Веса",
                Type = CompendiumFieldType.Dictionary,
                TargetKind = "stat",
                Step = 0.05,
            },
        ],
    };
}