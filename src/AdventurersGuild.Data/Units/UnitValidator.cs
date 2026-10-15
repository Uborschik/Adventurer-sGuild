namespace AdventurersGuild.Data.Adventurer;

using System.Collections.Generic;
using AdventurersGuild.Core;
using AdventurersGuild.Domain.Adventurers;

public static class UnitValidator
{
    public static void Validate()
    {
        IReadOnlyDictionary<string, ClassInfo> db = UnitDatabase.Classes;   // для удобства чтения
        var errors = 0;

        // === Расы ===
        foreach (var race in UnitDatabase.Races.Values)
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
                if (!UnitDatabase.Classes.ContainsKey(classId))
                {
                    Log.Error($"[AdventurerDB] Раса '{race.Id}': неизвестный класс '{classId}'");
                    errors++;
                }
            }

            if (race.StatBonuses != null)
            {
                foreach (var statId in race.StatBonuses.Keys)
                {
                    if (!UnitDatabase.Stats.ContainsKey(statId))
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
        foreach (ClassInfo cls in UnitDatabase.Classes.Values)
        {
            if (string.IsNullOrEmpty(cls.PrimaryStat) || !UnitDatabase.Stats.ContainsKey(cls.PrimaryStat))
            {
                Log.Error($"[AdventurerDB] Класс '{cls.Id}': primaryStat '{cls.PrimaryStat}' не найден");
                errors++;
            }

            if (string.IsNullOrEmpty(cls.SecondaryStat) || !UnitDatabase.Stats.ContainsKey(cls.SecondaryStat))
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

                foreach (KeyValuePair<string, double> kv in cls.GrowthWeights)
                {
                    if (!UnitDatabase.Stats.ContainsKey(kv.Key))
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
        foreach (UnitTemplateInfo t in UnitDatabase.Templates)
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
                     $"({UnitDatabase.Classes.Count} классов, " +
                     $"{UnitDatabase.Races.Count} рас, " +
                     $"{UnitDatabase.Stats.Count} статов, " +
                     $"{UnitDatabase.Statuses.Count} статусов, " +
                     $"{UnitDatabase.Templates.Count} шаблонов)");
        else
            Log.Error($"[AdventurerDB] Валидация: {errors} ошибок");
    }

    // === Валидация навыков ===

    public static void ValidateSkills()
    {
        int errors = 0;

        if (UnitDatabase.Skills == null || UnitDatabase.Skills.Count == 0)
        {
            Log.Error("[AdventurerDB] Навыки не загружены");
            return;
        }

        foreach (var skill in UnitDatabase.Skills.Values)
        {
            if (string.IsNullOrEmpty(skill.StatId))
            {
                Log.Error($"[AdventurerDB] Навык '{skill.Id}': не задан statId");
                errors++;
                continue;
            }

            if (!UnitDatabase.Stats.ContainsKey(skill.StatId))
            {
                Log.Error($"[AdventurerDB] Навык '{skill.Id}': стат '{skill.StatId}' не найден");
                errors++;
            }
        }

        foreach (var cls in UnitDatabase.Classes.Values)
        {
            if (!string.IsNullOrEmpty(cls.PrimarySkill))
            {
                string statId = UnitDatabase.StatForSkill(cls.PrimarySkill);

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
                string statId = UnitDatabase.StatForSkill(cls.SecondarySkill);

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
        }

        if (errors == 0)
            Log.Info($"[AdventurerDB] Валидация навыков: ok ({UnitDatabase.Skills.Count} навыков)");
        else
            Log.Error($"[AdventurerDB] Валидация навыков: {errors} ошибок");
    }

    public static void ValidateNames()
    {
        int errors = 0;

        foreach (var stat in UnitDatabase.Stats.Values)
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

        foreach (var race in UnitDatabase.Races.Values)
        {
            if (string.IsNullOrEmpty(race.Name.Ru) || string.IsNullOrEmpty(race.Name.En))
            {
                Log.Error($"[AdventurerDB] Раса '{race.Id}': неполное name");
                errors++;
            }
        }

        foreach (var cls in UnitDatabase.Classes.Values)
        {
            if (string.IsNullOrEmpty(cls.Name.Ru) || string.IsNullOrEmpty(cls.Name.En))
            {
                Log.Error($"[AdventurerDB] Класс '{cls.Id}': неполное name");
                errors++;
            }
        }

        foreach (var status in UnitDatabase.Statuses.Values)
        {
            if (string.IsNullOrEmpty(status.Name.Ru) || string.IsNullOrEmpty(status.Name.En))
            {
                Log.Error($"[AdventurerDB] Статус '{status.Id}': неполное name");
                errors++;
            }
        }

        var names = UnitDatabase.Names;
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

        int total = UnitDatabase.Stats.Count + UnitDatabase.Races.Count
                  + UnitDatabase.Classes.Count + UnitDatabase.Statuses.Count;

        if (errors == 0)
            Log.Info($"[AdventurerDB] Валидация имён: ok ({total} сущностей, " +
                     $"{names.FirstNamesMale?.Count ?? 0}+{names.FirstNamesFemale?.Count ?? 0} имён, " +
                     $"{names.Surnames?.Count ?? 0} фамилий)");
        else
            Log.Error($"[AdventurerDB] Валидация имён: {errors} ошибок");
    }
}