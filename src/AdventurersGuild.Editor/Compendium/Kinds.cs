#nullable enable

namespace AdventurersGuild.Editor.Compendium;

public static class Kinds
{
    public static readonly IReadOnlyList<KindInfo> All = new[]
    {
        // Мир
        new KindInfo { Kind = "stat", SingularTitle = "Стат", PluralTitle = "Статы",
            Section = "Мир/Статы", Order = 10,
            File = "Data/Units/Stats.json", RootPath = "stats" },

        new KindInfo { Kind = "phase", SingularTitle = "Фаза", PluralTitle = "Фазы",
            Section = "Мир/Фазы", Order = 20,
            File = "Data/Units/Phases.json", RootPath = "phases" },

        new KindInfo { Kind = "location", SingularTitle = "Локация", PluralTitle = "Локации",
            Section = "Мир/Локации", Order = 30,
            File = "Data/Quest/Locations.json", RootPath = "locations" },

        new KindInfo { Kind = "bestiary", SingularTitle = "Обитатель", PluralTitle = "Обитатели",
            Section = "Мир/Бестиарий", Order = 40,
            File = "Data/Units/Bestiary.json", RootPath = "habitats",
            IdField = "locationId", DisplayField = "locationId" },

        // Расы → Гуманоиды
        new KindInfo { Kind = "humanoid-species", SingularTitle = "Раса", PluralTitle = "Расы",
            Section = "Расы/Гуманоиды", Order = 10,
            File = "Data/Units/HumanoidSpecies.json", RootPath = "species" },

        new KindInfo { Kind = "humanoid-class", SingularTitle = "Класс", PluralTitle = "Классы",
            Section = "Расы/Гуманоиды", Order = 11,
            File = "Data/Units/HumanoidClasses.json", RootPath = "classes" },

        new KindInfo { Kind = "humanoid-role", SingularTitle = "Роль", PluralTitle = "Роли",
            Section = "Расы/Гуманоиды", Order = 12,
            File = "Data/Units/HumanoidRoles.json", RootPath = "roles" },

        // Расы → Звери
        new KindInfo { Kind = "beast-species", SingularTitle = "Вид", PluralTitle = "Виды",
            Section = "Расы/Звери", Order = 10,
            File = "Data/Units/BeastSpecies.json", RootPath = "species" },

        new KindInfo { Kind = "beast-class", SingularTitle = "Класс", PluralTitle = "Классы",
            Section = "Расы/Звери", Order = 11,
            File = "Data/Units/BeastClasses.json", RootPath = "classes" },

        new KindInfo { Kind = "beast-role", SingularTitle = "Роль", PluralTitle = "Роли",
            Section = "Расы/Звери", Order = 12,
            File = "Data/Units/BeastRoles.json", RootPath = "roles" },

        // Квесты
        new KindInfo { Kind = "grade", SingularTitle = "Градация", PluralTitle = "Градации",
            Section = "Квесты", Order = 10,
            File = "Data/Quest/QuestGrades.json", RootPath = "grades",
            IdField = "role", DisplayField = "role" },

        new KindInfo { Kind = "tag", SingularTitle = "Тег", PluralTitle = "Теги",
            Section = "Квесты", Order = 20,
            File = "Data/Quest/Tags.json", RootPath = "tags" },

        // Прочее
        new KindInfo { Kind = "debug-test", SingularTitle = "Тест", PluralTitle = "Тесты",
            Section = "Прочее", Order = 99,
            File = "Data/Debug/TestDictionary.json", RootPath = "items" },
    };

    public static KindInfo? Get(string kind)
        => All.FirstOrDefault(k => k.Kind == kind);
}