#nullable enable

namespace AdventurersGuild.Editor.Compendium.Schemas;

using AdventurersGuild.Editor.Compendium.Schema;

public static class GradeSchema
{
    public static EntitySchema Build() => new()
    {
        Kind = "grade",
        SingularTitle = "Градация",
        PluralTitle = "Градации",
        Section = "Квесты",
        Order = 20,
        File = "Data/Quest/QuestGrades.json",
        RootPath = "grades",
        DisplayField = "role",

        Fields = new[]
        {
            new FieldSchema
            {
                Name = "role",
                Label = "Роль",
                Hint = "disaster / failure / success / triumph",
                Type = CompendiumFieldType.Enum,
                Values = new[] { "disaster", "failure", "success", "triumph" },
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
                Name = "goldMultiplier",
                Label = "Множитель золота",
                Type = CompendiumFieldType.Number,
                Required = true,
                Step = 0.1,
                Group = "Механика",
                Order = 3,
                Width = 6,
            },
            new FieldSchema
            {
                Name = "gloryMultiplier",
                Label = "Множитель славы",
                Type = CompendiumFieldType.Number,
                Required = true,
                Step = 0.1,
                Group = "Механика",
                Order = 4,
                Width = 6,
            },
            new FieldSchema
            {
                Name = "countsAsSuccess",
                Label = "Считается успехом",
                Type = CompendiumFieldType.Bool,
                Required = true,
                Group = "Механика",
                Order = 5,
                Width = 6,
            },
        },
    };
}