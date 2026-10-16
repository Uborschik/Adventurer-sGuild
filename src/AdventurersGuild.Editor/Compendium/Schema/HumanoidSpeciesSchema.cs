#nullable enable

namespace AdventurersGuild.Editor.Compendium.Schemas;

using AdventurersGuild.Editor.Compendium.Schema;

public static class HumanoidSpeciesSchema
{
    public const string ClassRefSchemaName = "humanoid-species-class-ref";
    public const string RoleRefSchemaName = "humanoid-species-role-ref";

    public static EntitySchema Build() => new()
    {
        Kind = "humanoid-species",
        SingularTitle = "Раса",
        PluralTitle = "Расы",
        Section = "Мир/Бестиарий/Виды/Гуманоиды/Расы",
        Order = 10,
        File = "Data/Units/HumanoidSpecies.json",
        RootPath = "species",
        DisplayField = "name",

        Fields =
        [
            new FieldSchema { Name = "id", Label = "ID", Hint = "a-z, 0-9, _", Type = CompendiumFieldType.Id, Required = true, Group = "Основное", Order = 1, Width = 6 },
            new FieldSchema { Name = "name", Label = "Название", Type = CompendiumFieldType.LocalizedCase, Required = true, Group = "Основное", Order = 2, Width = 12 },
            new FieldSchema { Name = "statBonuses", Label = "Бонусы к статам", Hint = "Аддитивные бонусы", Type = CompendiumFieldType.Dictionary, TargetKind = "stat", Step = 1, Group = "Статы", Order = 10, Width = 12 },
            new FieldSchema { Name = "growthWeights", Label = "Веса роста", Hint = "Как быстро растут статы с уровнем", Type = CompendiumFieldType.Dictionary, TargetKind = "stat", Step = 0.05, Group = "Статы", Order = 11, Width = 12 },
            new FieldSchema { Name = "classes", Label = "Доступные классы", Type = CompendiumFieldType.ObjectList, NestedSchemaName = ClassRefSchemaName, Group = "Связи", Order = 20, Width = 12 },
            new FieldSchema { Name = "roles", Label = "Доступные роли", Type = CompendiumFieldType.ObjectList, NestedSchemaName = RoleRefSchemaName, Group = "Связи", Order = 21, Width = 12 },
        ],
    };

    public static EntitySchema BuildClassRef() => new()
    {
        Kind = ClassRefSchemaName,
        SingularTitle = "Класс",
        PluralTitle = "Классы",
        DisplayField = "classId",
        IdField = "classId",

        Fields =
        [
            new FieldSchema { Name = "classId", Label = "Класс", Type = CompendiumFieldType.Reference, TargetKind = "humanoid-class", Required = true, Group = "Основное", Order = 1, Width = 6 },
            new FieldSchema { Name = "weight", Label = "Вес", Type = CompendiumFieldType.Number, Step = 1, Required = true, Group = "Основное", Order = 2, Width = 6 },
            new FieldSchema { Name = "minLvl", Label = "Мин. уровень", Type = CompendiumFieldType.Number, Step = 1, Required = true, Group = "Диапазон", Order = 3, Width = 6 },
            new FieldSchema { Name = "maxLvl", Label = "Макс. уровень", Type = CompendiumFieldType.Number, Step = 1, Required = true, Group = "Диапазон", Order = 4, Width = 6 },
        ],
    };

    public static EntitySchema BuildRoleRef() => new()
    {
        Kind = RoleRefSchemaName,
        SingularTitle = "Роль",
        PluralTitle = "Роли",
        DisplayField = "roleId",
        IdField = "roleId",

        Fields =
        [
            new FieldSchema { Name = "roleId", Label = "Роль", Type = CompendiumFieldType.Reference, TargetKind = "humanoid-role", Required = true, Group = "Основное", Order = 1, Width = 8 },
            new FieldSchema { Name = "weight", Label = "Вес", Type = CompendiumFieldType.Number, Step = 1, Required = true, Group = "Основное", Order = 2, Width = 4 },
        ],
    };
}