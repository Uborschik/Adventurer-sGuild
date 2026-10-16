#nullable enable

namespace AdventurersGuild.Editor.Compendium.Schemas;

using AdventurersGuild.Editor.Compendium.Schema;

public static class BeastClassSchema
{
    public static EntitySchema Build() => new()
    {
        Kind = "beast-class",
        SingularTitle = "Класс",
        PluralTitle = "Классы",
        Section = "Мир/Бестиарий/Виды/Звери/Классы",
        Order = 21,
        File = "Data/Units/BeastClasses.json",
        RootPath = "classes",
        DisplayField = "name",

        Fields =
        [
            new FieldSchema { Name = "id", Label = "ID", Type = CompendiumFieldType.Id, Required = true, Group = "Основное", Order = 1, Width = 6 },
            new FieldSchema { Name = "name", Label = "Название", Type = CompendiumFieldType.Localized, Required = true, Group = "Основное", Order = 2, Width = 12 },
            new FieldSchema { Name = "statModifiers", Label = "Модификаторы статов", Type = CompendiumFieldType.Dictionary, TargetKind = "stat", Step = 0.05, Group = "Статы", Order = 10, Width = 12 },
        ],
    };
}