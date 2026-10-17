#nullable enable

namespace AdventurersGuild.Editor.Compendium;

public static class Kinds
{
    public static readonly IReadOnlyList<KindInfo> All =
    [
        // Система
        new KindInfo { Kind = "status", SingularTitle = "Статус", PluralTitle = "Статусы",
            Section = "Система/Статусы", Order = 5,
            File = "Data/Units/Statuses.json", RootPath = "statuses" },

        // Мир
        new KindInfo { Kind = "location", SingularTitle = "Локация", PluralTitle = "Локации",
            Section = "Мир/Локации", Order = 5,
            File = "Data/Quest/Locations.json", RootPath = "locations" },

        new KindInfo { Kind = "bestiary", SingularTitle = "Обитатель", PluralTitle = "Обитатели",
            Section = "Мир/Локации", Order = 10,
            File = "Data/Quest/Bestiary.json", RootPath = "habitats",
            IdField = "locationId", DisplayField = "locationId" },

        // Расы → Гуманоиды
        new KindInfo { Kind = "stat", SingularTitle = "Стат", PluralTitle = "Статы",
            Section = "Расы/Статы", Order = 5,
            File = "Data/Units/Stats.json", RootPath = "stats" },

        new KindInfo { Kind = "skill", SingularTitle = "Навык", PluralTitle = "Навыки",
            Section = "Расы/Навыки", Order = 10,
            File = "Data/Units/Skills.json", RootPath = "skills" },

        new KindInfo { Kind = "humanoid-species", SingularTitle = "Раса", PluralTitle = "Расы",
            Section = "Расы/Гуманоиды", Order = 10,
            File = "Data/Units/HumanoidSpecies.json", RootPath = "species" },

        new KindInfo { Kind = "humanoid-class", SingularTitle = "Класс", PluralTitle = "Классы",
            Section = "Расы/Гуманоиды", Order = 20,
            File = "Data/Units/HumanoidClasses.json", RootPath = "classes" },

        // Расы → Звери
        new KindInfo { Kind = "beast-species", SingularTitle = "Вид", PluralTitle = "Виды",
            Section = "Расы/Звери", Order = 5,
            File = "Data/Units/BeastSpecies.json", RootPath = "species" },

        new KindInfo { Kind = "beast-class", SingularTitle = "Класс", PluralTitle = "Классы",
            Section = "Расы/Звери", Order = 10,
            File = "Data/Units/BeastClasses.json", RootPath = "classes" },

        // Квесты
        new KindInfo { Kind = "phase", SingularTitle = "Фаза", PluralTitle = "Фазы",
            Section = "Квесты/Фазы", Order = 5,
            File = "Data/Units/Phases.json", RootPath = "phases" },

        new KindInfo { Kind = "quest-template", SingularTitle = "Шаблон", PluralTitle = "Шаблоны",
            Section = "Квесты/Шаблоны", Order = 10,
            File = "Data/Quest/QuestTemplates.json", RootPath = "templates" },

        new KindInfo { Kind = "role", SingularTitle = "Роль", PluralTitle = "Роли",
            Section = "Квесты/Актёры", Order = 20,
            File = "Data/Units/Roles.json", RootPath = "roles" },

        new KindInfo { Kind = "instruction", SingularTitle = "Инструкция", PluralTitle = "Инструкции",
            Section = "Квесты/Инструкции", Order = 30,
            File = "Data/Units/Instructions.json", RootPath = "instructions", DisplayField = "id" },

        new KindInfo { Kind = "grade", SingularTitle = "Градация", PluralTitle = "Градации",
            Section = "Квесты/Градации", Order = 40,
            File = "Data/Quest/QuestGrades.json", RootPath = "grades",
            IdField = "role", DisplayField = "role" },
    ];

    public static KindInfo? Get(string kind)
        => All.FirstOrDefault(k => k.Kind == kind);
}