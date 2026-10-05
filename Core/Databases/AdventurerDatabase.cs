using System.Collections.Generic;
using System.Linq;

public static class AdventurerDatabase
{
    private const string DataPath = "res://Resources/Data/Adventurer/";

    // === Публичные данные ===

    public static IReadOnlyDictionary<string, AdventurerClassInfo> Classes { get; private set; }
    public static IReadOnlyDictionary<string, AdventurerRaceInfo> Races { get; private set; }
    public static IReadOnlyDictionary<string, AdventurerStatInfo> Stats { get; private set; }
    public static IReadOnlyDictionary<string, AdventurerStatusInfo> Statuses { get; private set; }
    public static IReadOnlyList<AdventurerTemplateInfo> Templates { get; private set; }
    public static NameDatabase Names { get; private set; }

    // === Загрузка ===

    public static void Load()
    {
        LoadClasses();
        LoadRaces();
        LoadStats();
        LoadStatuses();
        LoadTemplates();
        LoadNames();

        Validate();
        ValidateNames();
    }

    private static void LoadClasses()
    {
        Classes = JsonLoader.Load<AdventurerClassDatabase>(DataPath + "AdventurerClasses.json")
            .Classes.ToDictionary(c => c.Id);
    }

    private static void LoadRaces()
    {
        Races = JsonLoader.Load<AdventurerRaceDatabase>(DataPath + "AdventurerRaces.json")
            .Races.ToDictionary(r => r.Id);
    }

    private static void LoadStats()
    {
        Stats = JsonLoader.Load<AdventurerStatDatabase>(DataPath + "AdventurerStats.json")
            .Stats.ToDictionary(s => s.Id);
    }

    private static void LoadStatuses()
    {
        Statuses = JsonLoader.Load<AdventurerStatusDatabase>(DataPath + "AdventurerStatuses.json")
            .Statuses.ToDictionary(s => s.Id);
    }

    private static void LoadTemplates()
    {
        Templates = JsonLoader.Load<AdventurerTemplateDatabase>(DataPath + "AdventurerTemplates.json")
            .Templates;
    }

    private static void LoadNames()
    {
        Names = JsonLoader.Load<NameDatabase>(DataPath + "AdventurerNames.json");
    }

    // === Публичное API — геттеры ===

    public static AdventurerClassInfo GetClass(string id) => Lookup(Classes, id, "класс");
    public static AdventurerRaceInfo GetRace(string id) => Lookup(Races, id, "раса");
    public static AdventurerStatInfo GetStat(string id) => Lookup(Stats, id, "стат");
    public static AdventurerStatusInfo GetStatus(string id) => Lookup(Statuses, id, "статус");

    // === Публичное API — локализованные имена ===

    public static string ClassName(string id)
    {
        var c = GetClass(id);
        return c?.Name.Get(Loc.Language) ?? id;
    }

    public static string RaceName(string id)
    {
        var r = GetRace(id);
        return r?.Name.Get(Loc.Language) ?? id;
    }

    public static string StatName(string id)
    {
        var s = GetStat(id);
        return s?.Name.Get(Loc.Language) ?? id;
    }

    public static string StatDescription(string id)
    {
        var s = GetStat(id);
        return s?.Description.Get(Loc.Language) ?? "";
    }

    public static string StatusName(string id)
    {
        var s = GetStatus(id);
        return s?.Name.Get(Loc.Language) ?? id;
    }

    // === Публичное API — прочее ===

    public static bool IsFree(AdventurerModel a)
    {
        if (a == null) return false;
        var info = GetStatus(a.StatusId);
        return info == null || !info.BlocksAssignment;
    }

    // === Валидация ===

    private static void Validate()
    {
        int errors = 0;

        // === Расы ===
        foreach (var race in Races.Values)
        {
            if (race.Weight == null)
            {
                Log.Error($"[AdventurerDB] Раса '{race.Id}': нет weight");
                errors++;
                continue;
            }

            if (race.Weight.Population <= 0)
                Log.Warn($"[AdventurerDB] Раса '{race.Id}': population = 0, никогда не появится");

            if (race.Weight.Classes == null || race.Weight.Classes.Count == 0)
            {
                Log.Error($"[AdventurerDB] Раса '{race.Id}': пустой список classes");
                errors++;
                continue;
            }

            foreach (var classId in race.Weight.Classes.Keys)
            {
                if (!Classes.ContainsKey(classId))
                {
                    Log.Error($"[AdventurerDB] Раса '{race.Id}': неизвестный класс '{classId}'");
                    errors++;
                }
            }

            if (race.StatBonuses != null)
            {
                foreach (var statId in race.StatBonuses.Keys)
                {
                    if (!Stats.ContainsKey(statId))
                    {
                        Log.Error($"[AdventurerDB] Раса '{race.Id}': неизвестный стат '{statId}' в statBonuses");
                        errors++;
                    }
                }
            }

            if (race.ResilienceMultiplier <= 0)
            {
                Log.Error($"[AdventurerDB] Раса '{race.Id}': resilienceMultiplier <= 0");
                errors++;
            }
        }

        // === Классы ===
        foreach (var cls in Classes.Values)
        {
            if (string.IsNullOrEmpty(cls.PrimaryStat) || !Stats.ContainsKey(cls.PrimaryStat))
            {
                Log.Error($"[AdventurerDB] Класс '{cls.Id}': primaryStat '{cls.PrimaryStat}' не найден");
                errors++;
            }

            if (string.IsNullOrEmpty(cls.SecondaryStat) || !Stats.ContainsKey(cls.SecondaryStat))
            {
                Log.Error($"[AdventurerDB] Класс '{cls.Id}': secondaryStat '{cls.SecondaryStat}' не найден");
                errors++;
            }

            if (!string.IsNullOrEmpty(cls.PrimaryStat) &&
                cls.PrimaryStat == cls.SecondaryStat)
            {
                Log.Error($"[AdventurerDB] Класс '{cls.Id}': primary == secondary ('{cls.PrimaryStat}')");
                errors++;
            }

            if (cls.GrowthWeights == null || cls.GrowthWeights.Count == 0)
            {
                Log.Error($"[AdventurerDB] Класс '{cls.Id}': пустой growthWeights");
                errors++;
            }
            else
            {
                foreach (var statId in StatIds.All)
                {
                    if (!cls.GrowthWeights.ContainsKey(statId))
                        Log.Warn($"[AdventurerDB] Класс '{cls.Id}': нет веса для '{statId}', будет 0.20");
                }

                foreach (var kv in cls.GrowthWeights)
                {
                    if (!Stats.ContainsKey(kv.Key))
                    {
                        Log.Error($"[AdventurerDB] Класс '{cls.Id}': неизвестный стат '{kv.Key}' в growthWeights");
                        errors++;
                    }
                    if (kv.Value < 0)
                    {
                        Log.Error($"[AdventurerDB] Класс '{cls.Id}': отрицательный вес '{kv.Key}' = {kv.Value}");
                        errors++;
                    }
                }

                // Primary должен иметь максимальный вес, а не просто существовать
                if (cls.GrowthWeights.TryGetValue(cls.PrimaryStat, out var pw))
                {
                    foreach (var kv in cls.GrowthWeights)
                    {
                        if (kv.Key != cls.PrimaryStat && kv.Value > pw)
                        {
                            Log.Warn($"[AdventurerDB] Класс '{cls.Id}': вес '{kv.Key}' ({kv.Value}) " +
                                           $"больше primary '{cls.PrimaryStat}' ({pw})");
                        }
                    }
                }
            }
        }

        // === Шаблоны ===
        foreach (var t in Templates)
        {
            if (t.MinLevel < 1)
            {
                Log.Error($"[AdventurerDB] Шаблон '{t.Id}': minLevel < 1");
                errors++;
            }

            if (t.MaxLevel < t.MinLevel)
            {
                Log.Error($"[AdventurerDB] Шаблон '{t.Id}': maxLevel < minLevel");
                errors++;
            }
        }

        // === Итог ===
        if (errors == 0)
            Log.Info($"[AdventurerDB] Валидация: ok ({Classes.Count} классов, {Races.Count} рас, " +
                     $"{Stats.Count} статов, {Statuses.Count} статусов, {Templates.Count} шаблонов)");
        else
            Log.Error($"[AdventurerDB] Валидация: {errors} ошибок");
    }

    private static void ValidateNames()
    {
        int errors = 0;

        // === Статы ===
        foreach (var stat in Stats.Values)
        {
            if (string.IsNullOrEmpty(stat.Name.Ru) || string.IsNullOrEmpty(stat.Name.En))
            {
                Log.Error($"[AdventurerDB] Стат '{stat.Id}': неполное name");
                errors++;
            }
            if (string.IsNullOrEmpty(stat.Description.Ru) || string.IsNullOrEmpty(stat.Description.En))
            {
                Log.Error($"[AdventurerDB] Стат '{stat.Id}': неполное description");
                errors++;
            }
        }

        // === Расы ===
        foreach (var race in Races.Values)
        {
            if (string.IsNullOrEmpty(race.Name.Ru) || string.IsNullOrEmpty(race.Name.En))
            {
                Log.Error($"[AdventurerDB] Раса '{race.Id}': неполное name");
                errors++;
            }
        }

        // === Классы ===
        foreach (var cls in Classes.Values)
        {
            if (string.IsNullOrEmpty(cls.Name.Ru) || string.IsNullOrEmpty(cls.Name.En))
            {
                Log.Error($"[AdventurerDB] Класс '{cls.Id}': неполное name");
                errors++;
            }
        }

        // === Статусы ===
        foreach (var status in Statuses.Values)
        {
            if (string.IsNullOrEmpty(status.Name.Ru) || string.IsNullOrEmpty(status.Name.En))
            {
                Log.Error($"[AdventurerDB] Статус '{status.Id}': неполное name");
                errors++;
            }
        }

        // === Имена авантюристов ===
        if (Names == null)
        {
            Log.Error("[AdventurerDB] Names не загружен");
            return;
        }

        if (Names.FirstNamesMale == null || Names.FirstNamesMale.Count == 0)
        {
            Log.Error("[AdventurerDB] Пустой firstNamesMale");
            errors++;
        }

        if (Names.FirstNamesFemale == null || Names.FirstNamesFemale.Count == 0)
        {
            Log.Error("[AdventurerDB] Пустой firstNamesFemale");
            errors++;
        }

        if (Names.Surnames == null || Names.Surnames.Count == 0)
        {
            Log.Error("[AdventurerDB] Пустой surnames");
            errors++;
        }

        // === Итог ===
        int total = Stats.Count + Races.Count + Classes.Count + Statuses.Count;
        if (errors == 0)
            Log.Info($"[AdventurerDB] Валидация имён: ok ({total} сущностей, " +
                     $"{Names.FirstNamesMale?.Count ?? 0}+{Names.FirstNamesFemale?.Count ?? 0} имён, " +
                     $"{Names.Surnames?.Count ?? 0} фамилий)");
        else
            Log.Error($"[AdventurerDB] Валидация имён: {errors} ошибок");
    }

    // === Приватные хелперы ===

    private static T Lookup<T>(IReadOnlyDictionary<string, T> dict, string id, string what) where T : class
    {
        if (dict != null && dict.TryGetValue(id, out var value)) return value;
        Log.Warn($"[AdventurerDB] Не найден {what}: {id}");
        return null;
    }
}