#nullable enable

namespace AdventurersGuild.Editor.Compendium.Schemas;

using AdventurersGuild.Editor.Compendium.Schema;

public static class HumanoidClassSchema
{
    public static EntitySchema Build() => new()
    {
        Kind = "humanoid-class",
        SingularTitle = "Класс",
        PluralTitle = "Классы",
        Section = "Мир/Бестиарий/Виды/Гуманоиды/Классы",
        Order = 11,
        File = "Data/Units/HumanoidClasses.json",
        RootPath = "classes",
        DisplayField = "name",

        Fields =
        [
            new FieldSchema { Name = "id", Label = "ID", Type = CompendiumFieldType.Id, Required = true, Group = "Основное", Order = 1, Width = 6 },
            new FieldSchema { Name = "name", Label = "Название", Type = CompendiumFieldType.Localized, Required = true, Group = "Основное", Order = 2, Width = 12 },
            new FieldSchema { Name = "primaryStat", Label = "Primary стат", Type = CompendiumFieldType.Reference, TargetKind = "stat", Required = false, Group = "Специализация", Order = 3, Width = 6 },
            new FieldSchema { Name = "primarySkill", Label = "Primary навык", Type = CompendiumFieldType.Reference, TargetKind = "skill", Required = false, Group = "Специализация", Order = 4, Width = 6 },
            new FieldSchema { Name = "secondaryStat", Label = "Secondary стат", Type = CompendiumFieldType.Reference, TargetKind = "stat", Required = false, Group = "Специализация", Order = 5, Width = 6 },
            new FieldSchema { Name = "secondarySkill", Label = "Secondary навык", Type = CompendiumFieldType.Reference, TargetKind = "skill", Required = false, Group = "Специализация", Order = 6, Width = 6 },
            new FieldSchema { Name = "statModifiers", Label = "Модификаторы статов", Type = CompendiumFieldType.Dictionary, TargetKind = "stat", Step = 0.05, Group = "Статы", Order = 10, Width = 12 },
        ],
    };
}