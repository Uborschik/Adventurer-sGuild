namespace AdventurersGuild.Data.Adventurer;

using AdventurersGuild.Core;
using AdventurersGuild.Domain.Adventurers;

public static class AdventurerValidator
{
    public static void Validate()
    {
        var db = AdventurerDatabase.Classes;   // для удобства чтения
        int errors = 0;

        // === Расы ===
        foreach (var race in AdventurerDatabase.Races.Values)
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
                if (!AdventurerDatabase.Classes.ContainsKey(classId))
                {
                    Log.Error($"[AdventurerDB] Раса '{race.Id}': неизвестный класс '{classId}'");
                    errors++;
                }
            }

            if (race.StatBonuses != null)
            {
                foreach (var statId in race.StatBonuses.Keys)
                {
                    if (!AdventurerDatabase.Stats.ContainsKey(statId))
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
        foreach (var cls in AdventurerDatabase.Classes.Values)
        {
            if (string.IsNullOrEmpty(cls.PrimaryStat) || !AdventurerDatabase.Stats.ContainsKey(cls.PrimaryStat))
            {
                Log.Error($"[AdventurerDB] Класс '{cls.Id}': primaryStat '{cls.PrimaryStat}' не найден");
                errors++;
            }

            if (string.IsNullOrEmpty(cls.SecondaryStat) || !AdventurerDatabase.Stats.ContainsKey(cls.SecondaryStat))
            {
                Log.Error($"[AdventurerDB] Класс '{cls.Id}': secondaryStat '{cls.SecondaryStat}' не найден");
                errors++;
            }

            if (!string.IsNullOrEmpty(cls.PrimaryStat) && cls.PrimaryStat == cls.SecondaryStat)
            {
                Log.Error($"[AdventurerDB] Класс '{cls.Id}': primary == secondary ('{cls.PrimaryStat}')");
                errors++;
            }

            if (string.IsNullOrEmpty(cls.PrimarySkill))
            {
                Log.Error($"[AdventurerDB] Класс '{cls.Id}': пустой primarySkill");
                errors++;
            }

            if (string.IsNullOrEmpty(cls.SecondarySkill))
            {
                Log.Error($"[AdventurerDB] Класс '{cls.Id}': пустой secondarySkill");
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
                    if (!AdventurerDatabase.Stats.ContainsKey(kv.Key))
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
        foreach (var t in AdventurerDatabase.Templates)
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
            Log.Info($"[AdventurerDB] Валидация: ok " +
                     $"({AdventurerDatabase.Classes.Count} классов, " +
                     $"{AdventurerDatabase.Races.Count} рас, " +
                     $"{AdventurerDatabase.Stats.Count} статов, " +
                     $"{AdventurerDatabase.Statuses.Count} статусов, " +
                     $"{AdventurerDatabase.Templates.Count} шаблонов)");
        else
            Log.Error($"[AdventurerDB] Валидация: {errors} ошибок");
    }

    // === Валидация навыков ===

    public static void ValidateSkills()
    {
        int errors = 0;

        foreach (var stat in AdventurerDatabase.Stats.Values)
        {
            if (stat.Skills == null || stat.Skills.Count == 0)
            {
                Log.Error($"[AdventurerDB] Стат '{stat.Id}': нет навыков");
                errors++;
            }
        }

        foreach (var cls in AdventurerDatabase.Classes.Values)
        {
            if (!string.IsNullOrEmpty(cls.PrimarySkill))
            {
                string statId = AdventurerDatabase.StatForSkill(cls.PrimarySkill);

                if (statId == null)
                {
                    Log.Error($"[AdventurerDB] Класс '{cls.Id}': primarySkill '{cls.PrimarySkill}' не найден");
                    errors++;
                }
                else if (statId != cls.PrimaryStat)
                {
                    Log.Error($"[AdventurerDB] Класс '{cls.Id}': primarySkill '{cls.PrimarySkill}' " +
                             $"принадлежит '{statId}', а primaryStat = '{cls.PrimaryStat}'");
                    errors++;
                }
            }

            if (!string.IsNullOrEmpty(cls.SecondarySkill))
            {
                string statId = AdventurerDatabase.StatForSkill(cls.SecondarySkill);

                if (statId == null)
                {
                    Log.Error($"[AdventurerDB] Класс '{cls.Id}': secondarySkill '{cls.SecondarySkill}' не найден");
                    errors++;
                }
                else if (statId != cls.SecondaryStat)
                {
                    Log.Error($"[AdventurerDB] Класс '{cls.Id}': secondarySkill '{cls.SecondarySkill}' " +
                             $"принадлежит '{statId}', а secondaryStat = '{cls.SecondaryStat}'");
                    errors++;
                }
            }

            if (cls.PrimarySkill == cls.SecondarySkill && !string.IsNullOrEmpty(cls.PrimarySkill))
            {
                Log.Error($"[AdventurerDB] Класс '{cls.Id}': primarySkill == secondarySkill");
                errors++;
            }
        }

        if (errors == 0)
            Log.Info($"[AdventurerDB] Валидация навыков: ok ({AdventurerDatabase.SkillCount} навыков)");
        else
            Log.Error($"[AdventurerDB] Валидация навыков: {errors} ошибок");
    }

    public static void ValidateNames()
    {
        int errors = 0;

        foreach (var stat in AdventurerDatabase.Stats.Values)
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

        foreach (var race in AdventurerDatabase.Races.Values)
        {
            if (string.IsNullOrEmpty(race.Name.Ru) || string.IsNullOrEmpty(race.Name.En))
            {
                Log.Error($"[AdventurerDB] Раса '{race.Id}': неполное name");
                errors++;
            }
        }

        foreach (var cls in AdventurerDatabase.Classes.Values)
        {
            if (string.IsNullOrEmpty(cls.Name.Ru) || string.IsNullOrEmpty(cls.Name.En))
            {
                Log.Error($"[AdventurerDB] Класс '{cls.Id}': неполное name");
                errors++;
            }
        }

        foreach (var status in AdventurerDatabase.Statuses.Values)
        {
            if (string.IsNullOrEmpty(status.Name.Ru) || string.IsNullOrEmpty(status.Name.En))
            {
                Log.Error($"[AdventurerDB] Статус '{status.Id}': неполное name");
                errors++;
            }
        }

        var names = AdventurerDatabase.Names;
        if (names == null)
        {
            Log.Error("[AdventurerDB] Names не загружен");
            return;
        }

        if (names.FirstNamesMale == null || names.FirstNamesMale.Count == 0)
        {
            Log.Error("[AdventurerDB] Пустой firstNamesMale");
            errors++;
        }

        if (names.FirstNamesFemale == null || names.FirstNamesFemale.Count == 0)
        {
            Log.Error("[AdventurerDB] Пустой firstNamesFemale");
            errors++;
        }

        if (names.Surnames == null || names.Surnames.Count == 0)
        {
            Log.Error("[AdventurerDB] Пустой surnames");
            errors++;
        }

        int total = AdventurerDatabase.Stats.Count + AdventurerDatabase.Races.Count
                  + AdventurerDatabase.Classes.Count + AdventurerDatabase.Statuses.Count;

        if (errors == 0)
            Log.Info($"[AdventurerDB] Валидация имён: ok ({total} сущностей, " +
                     $"{names.FirstNamesMale?.Count ?? 0}+{names.FirstNamesFemale?.Count ?? 0} имён, " +
                     $"{names.Surnames?.Count ?? 0} фамилий)");
        else
            Log.Error($"[AdventurerDB] Валидация имён: {errors} ошибок");
    }
}