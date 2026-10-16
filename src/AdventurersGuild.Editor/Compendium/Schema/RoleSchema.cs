#nullable enable

namespace AdventurersGuild.Editor.Compendium.Schemas;

using AdventurersGuild.Editor.Compendium.Schema;

public static class RoleSchema
{
    public const string ActionSchemaName = "role-action";
    public const string CheckSchemaName = "role-action-check";

    public static EntitySchema Build() => new()
    {
        Kind = "role",
        SingularTitle = "Роль",
        PluralTitle = "Роли",
        Section = "Мир",
        Order = 15,
        File = "Data/Units/Roles.json",
        RootPath = "roles",
        DisplayField = "name",

        Fields = new[]
        {
            new FieldSchema
            {
                Name = "id", Label = "ID", Hint = "a-z, 0-9, _",
                Type = CompendiumFieldType.Id, Required = true,
                Group = "Основное", Order = 1, Width = 6,
            },
            new FieldSchema
            {
                Name = "name", Label = "Название",
                Type = CompendiumFieldType.Localized, Required = true,
                Group = "Основное", Order = 2, Width = 12,
            },
            new FieldSchema
            {
                Name = "type", Label = "Тип",
                Type = CompendiumFieldType.Enum,
                Values = new[] { "simple", "composite" },
                Required = true, Group = "Основное", Order = 3, Width = 6,
            },
            new FieldSchema
            {
                Name = "actions", Label = "Действия",
                Type = CompendiumFieldType.ObjectList,
                NestedSchemaName = ActionSchemaName,
                Group = "Действия", Order = 10, Width = 12,
            },
        },
    };

    public static EntitySchema BuildAction() => new()
    {
        Kind = ActionSchemaName,
        SingularTitle = "Действие",
        PluralTitle = "Действия",
        DisplayField = "name",
        IdField = "id",

        Fields = new[]
        {
            new FieldSchema
            {
                Name = "id", Label = "ID",
                Type = CompendiumFieldType.Id, Required = true,
                Group = "Основное", Order = 1, Width = 6,
            },
            new FieldSchema
            {
                Name = "name", Label = "Название",
                Type = CompendiumFieldType.Localized, Required = true,
                Group = "Основное", Order = 2, Width = 12,
            },
            new FieldSchema
            {
                Name = "tags", Label = "Теги",
                Type = CompendiumFieldType.ReferenceList, TargetKind = "tag",
                Required = false, Group = "Связи", Order = 3, Width = 12,
            },
            new FieldSchema
            {
                Name = "phaseId", Label = "Фаза",
                Type = CompendiumFieldType.Reference, TargetKind = "phase",
                Required = true, Group = "Связи", Order = 4, Width = 6,
            },
            new FieldSchema
            {
                Name = "duration", Label = "Длительность (мин)",
                Type = CompendiumFieldType.Number, Step = 1,
                Required = true, Group = "Параметры", Order = 5, Width = 4,
            },
            new FieldSchema
            {
                Name = "exp", Label = "Опыт",
                Type = CompendiumFieldType.Number, Step = 1,
                Required = true, Group = "Параметры", Order = 6, Width = 4,
            },
            new FieldSchema
            {
                Name = "weight", Label = "Вес",
                Type = CompendiumFieldType.Number, Step = 1,
                Required = true, Group = "Параметры", Order = 7, Width = 4,
            },
            new FieldSchema
            {
                Name = "check", Label = "Проверка",
                Type = CompendiumFieldType.Nested,
                NestedSchemaName = CheckSchemaName,
                Required = true, Group = "Проверка", Order = 10, Width = 12,
            },
        },
    };

    public static EntitySchema BuildCheck() => new()
    {
        Kind = CheckSchemaName,
        SingularTitle = "Проверка",
        PluralTitle = "Проверки",
        DisplayField = "selfStat",
        IdField = "selfStat",

        Fields = new[]
        {
            new FieldSchema
            {
                Name = "selfStat", Label = "Свой стат",
                Type = CompendiumFieldType.Reference, TargetKind = "stat",
                Required = true, Group = "Основное", Order = 1, Width = 6,
            },
            new FieldSchema
            {
                Name = "against", Label = "Против чего",
                Type = CompendiumFieldType.Enum,
                Values = new[]
                {
                    "stat:str", "stat:dex", "stat:end",
                    "stat:int", "stat:wis", "stat:cha",
                    "creature", "none",
                },
                Required = true, Group = "Основное", Order = 2, Width = 6,
            },
        },
    };
}