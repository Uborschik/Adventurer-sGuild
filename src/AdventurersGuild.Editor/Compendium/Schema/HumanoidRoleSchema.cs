#nullable enable

namespace AdventurersGuild.Editor.Compendium.Schemas;

using AdventurersGuild.Editor.Compendium.Schema;

public static class HumanoidRoleSchema
{
    public const string StateSchemaName = "humanoid-role-state";
    public const string ActionSchemaName = "humanoid-role-action";
    public const string CheckSchemaName = "humanoid-role-check";

    public static EntitySchema Build() => new()
    {
        Kind = "humanoid-role",
        SingularTitle = "Роль",
        PluralTitle = "Роли",
        Section = "Мир/Бестиарий/Виды/Гуманоиды/Роли",
        Order = 12,
        File = "Data/Units/HumanoidRoles.json",
        RootPath = "roles",
        DisplayField = "name",

        Fields =
        [
            new FieldSchema { Name = "id", Label = "ID", Type = CompendiumFieldType.Id, Required = true, Group = "Основное", Order = 1, Width = 6 },
            new FieldSchema { Name = "name", Label = "Название", Type = CompendiumFieldType.Localized, Required = true, Group = "Основное", Order = 2, Width = 12 },
            new FieldSchema { Name = "states", Label = "Состояния", Type = CompendiumFieldType.ObjectList, NestedSchemaName = StateSchemaName, Required = true, Group = "Состояния", Order = 10, Width = 12 },
        ],
    };

    public static EntitySchema BuildState() => new()
    {
        Kind = StateSchemaName,
        SingularTitle = "Состояние",
        PluralTitle = "Состояния",
        DisplayField = "name",
        IdField = "id",

        Fields =
        [
            new FieldSchema { Name = "id", Label = "ID", Type = CompendiumFieldType.Id, Required = true, Group = "Основное", Order = 1, Width = 6 },
            new FieldSchema { Name = "name", Label = "Название", Type = CompendiumFieldType.Localized, Required = true, Group = "Основное", Order = 2, Width = 12 },
            new FieldSchema
            {
                Name = "success", Label = "При успехе",
                Hint = "ID другого состояния или 'end'",
                Type = CompendiumFieldType.SiblingRef,
                OptionsFromParentArray = "states",
                Required = true, Group = "Переходы", Order = 3, Width = 6,
            },
            new FieldSchema
            {
                Name = "fail", Label = "При провале",
                Hint = "ID другого состояния или 'end'",
                Type = CompendiumFieldType.SiblingRef,
                OptionsFromParentArray = "states",
                Required = true, Group = "Переходы", Order = 4, Width = 6,
            },
            new FieldSchema { Name = "actions", Label = "Действия", Type = CompendiumFieldType.ObjectList, NestedSchemaName = ActionSchemaName, Group = "Действия", Order = 10, Width = 12 },
        ],
    };

    public static EntitySchema BuildAction() => new()
    {
        Kind = ActionSchemaName,
        SingularTitle = "Действие",
        PluralTitle = "Действия",
        DisplayField = "name",
        IdField = "id",

        Fields =
        [
            new FieldSchema { Name = "id", Label = "ID", Type = CompendiumFieldType.Id, Required = true, Group = "Основное", Order = 1, Width = 6 },
            new FieldSchema { Name = "name", Label = "Название", Type = CompendiumFieldType.Localized, Required = true, Group = "Основное", Order = 2, Width = 12 },
            new FieldSchema { Name = "phaseId", Label = "Фаза", Type = CompendiumFieldType.Reference, TargetKind = "phase", Required = true, Group = "Связи", Order = 3, Width = 6 },
            new FieldSchema { Name = "duration", Label = "Длительность (мин)", Type = CompendiumFieldType.Number, Step = 1, Required = true, Group = "Параметры", Order = 4, Width = 6 },
            new FieldSchema { Name = "check", Label = "Проверка", Type = CompendiumFieldType.Nested, NestedSchemaName = CheckSchemaName, Required = false, Group = "Проверка", Order = 10, Width = 12 },
        ],
    };

    public static EntitySchema BuildCheck() => new()
    {
        Kind = CheckSchemaName,
        SingularTitle = "Проверка",
        PluralTitle = "Проверки",
        DisplayField = "selfStat",
        IdField = "selfStat",

        Fields =
        [
            new FieldSchema { Name = "selfStat", Label = "Свой стат", Type = CompendiumFieldType.Reference, TargetKind = "stat", Required = true, Group = "Основное", Order = 1, Width = 6 },
            new FieldSchema
            {
                Name = "against", Label = "Против чего",
                Type = CompendiumFieldType.Enum,
                Values = new[] { "stat:str", "stat:dex", "stat:end", "stat:int", "stat:wis", "stat:cha", "creature", "none" },
                Required = true, Group = "Основное", Order = 2, Width = 6,
            },
        ],
    };
}