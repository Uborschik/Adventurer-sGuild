namespace AdventurersGuild.Editor.Compendium.Schemas;

using AdventurersGuild.Editor.Compendium.Schema;

public static class TagSchema
{
    public static EntitySchema Build() => new()
    {
        Kind = "tag",
        SingularTitle = "Тег",
        PluralTitle = "Теги",
        Section = "Квесты",
        Order = 30,
        File = "Data/Quest/Tags.json",
        RootPath = "tags",
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
                Type = CompendiumFieldType.Localized,
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
        },
    };
}