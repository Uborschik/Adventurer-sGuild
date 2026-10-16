#nullable enable

namespace AdventurersGuild.Editor.Compendium.Schemas;

using AdventurersGuild.Editor.Compendium.Schema;

public static class LocationSchema
{
    public static EntitySchema Build() => new()
    {
        Kind = "location",
        SingularTitle = "Локация",
        PluralTitle = "Локации",
        Section = "Мир/Локации",
        Order = 10,
        File = "Data/Quest/Locations.json",
        RootPath = "locations",
        DisplayField = "name",

        Fields = new[]
        {
            new FieldSchema
            {
                Name = "id",
                Label = "ID",
                Hint = "a-z, 0-9, _",
                Type = CompendiumFieldType.Id,
                Required = true,
                Group = "Основное",
                Order = 1,
                Width = 6,
            },
            new FieldSchema
            {
                Name = "name",
                Label = "Название",
                Type = CompendiumFieldType.LocalizedCase,
                Required = true,
                Group = "Основное",
                Order = 2,
                Width = 12,
            },
            new FieldSchema
            {
                Name = "description",
                Label = "Описание",
                Type = CompendiumFieldType.Localized,
                Required = false,
                Group = "Основное",
                Order = 3,
                Width = 12,
            },
            new FieldSchema
            {
                Name = "difficultyModifier",
                Label = "Модификатор сложности",
                Hint = "Множитель, прибавляется к DC фаз",
                Type = CompendiumFieldType.Number,
                Required = true,
                Step = 0.05,
                Group = "Механика",
                Order = 4,
                Width = 6,
            },
            new FieldSchema
            {
                Name = "travelDaysModifier",
                Label = "Дорога от гильдии (дней)",
                Type = CompendiumFieldType.Number,
                Required = true,
                Step = 1,
                Group = "Механика",
                Order = 5,
                Width = 6,
            },
        },
    };
}