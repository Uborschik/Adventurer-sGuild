namespace AdventurersGuild.Data.Adventurer;

using AdventurersGuild.Core;
using AdventurersGuild.Domain.Adventurers;

public static class UnitValidator
{
    public static void Validate()
    {
        var errors = 0;

        // === Виды (Species) ===
        foreach (var species in UnitDatabase.Species.Values)
        {
            if (species.Classes == null || species.Classes.Count == 0)
            {
                Log.Error($"[AdventurerDB] Вид '{species.Id}': пустой список classes");
                errors++;
                continue;
            }

            foreach (var sc in species.Classes)
            {
                if (!UnitDatabase.Classes.ContainsKey(sc.ClassId))
                {
                    Log.Error($"[AdventurerDB] Вид '{species.Id}': неизвестный класс '{sc.ClassId}'");
                    errors++;
                }
                if (sc.Weight <= 0)
                {
                    Log.Error($"[AdventurerDB] Вид '{species.Id}', класс '{sc.ClassId}': weight <= 0");
                    errors++;
                }
                if (sc.MinLvl < 1 || sc.MaxLvl < sc.MinLvl)
                {
                    Log.Error($"[AdventurerDB] Вид '{species.Id}', класс '{sc.ClassId}': некорректный диапазон уровней ({sc.MinLvl}-{sc.MaxLvl})");
                    errors++;
                }
            }

            if (species.StatBonuses != null)
            {
                foreach (var statId in species.StatBonuses.Keys)
                {
                    if (!UnitDatabase.Stats.ContainsKey(statId))
                    {
                        Log.Error($"[AdventurerDB] Вид '{species.Id}': неизвестный стат '{statId}' в statBonuses");
                        errors++;
                    }
                }
            }

            if (species.GrowthWeights != null)
            {
                foreach (var kv in species.GrowthWeights)
                {
                    if (!UnitDatabase.Stats.ContainsKey(kv.Key))
                    {
                        Log.Error($"[AdventurerDB] Вид '{species.Id}': неизвестный стат '{kv.Key}' в growthWeights");
                        errors++;
                    }
                    if (kv.Value < 0)
                    {
                        Log.Error($"[AdventurerDB] Вид '{species.Id}': отрицательный вес '{kv.Key}' = {kv.Value}");
                        errors++;
                    }
                }
            }
        }

        // === Классы ===
        foreach (ClassInfo cls in UnitDatabase.Classes.Values)
        {
            var isBeast = string.IsNullOrEmpty(cls.PrimaryStat);

            if (!isBeast)
            {
                if (!UnitDatabase.Stats.ContainsKey(cls.PrimaryStat))
                {
                    Log.Error($"[AdventurerDB] Класс '{cls.Id}': primaryStat '{cls.PrimaryStat}' не найден");
                    errors++;
                }

                if (!UnitDatabase.Stats.ContainsKey(cls.SecondaryStat))
                {
                    Log.Error($"[AdventurerDB] Класс '{cls.Id}': secondaryStat '{cls.SecondaryStat}' не найден");
                    errors++;
                }

                if (cls.PrimaryStat == cls.SecondaryStat)
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
            }

            if (cls.ClassCost <= 0)
            {
                Log.Error($"[AdventurerDB] Класс '{cls.Id}': classCost должен быть > 0 (текущее: {cls.ClassCost})");
                errors++;
            }

            if (cls.StatModifiers != null)
            {
                foreach (var kv in cls.StatModifiers)
                {
                    if (!UnitDatabase.Stats.ContainsKey(kv.Key))
                    {
                        Log.Error($"[AdventurerDB] Класс '{cls.Id}': неизвестный стат '{kv.Key}' в statModifiers");
                        errors++;
                    }
                }
            }
        }
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
            // Пропускаем зверей, у них нет навыков
            if (string.IsNullOrEmpty(cls.PrimaryStat)) continue;

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

        foreach (var species in UnitDatabase.Species.Values)
        {
            var name = species.Name;

            if (name.Ru == null || name.En == null || string.IsNullOrEmpty(name.Ru.Nom) || string.IsNullOrEmpty(name.En))
            {
                Log.Error($"[AdventurerDB] Вид '{species.Id}': неполное name");
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

        int total = UnitDatabase.Stats.Count + UnitDatabase.Species.Count
                  + UnitDatabase.Classes.Count + UnitDatabase.Statuses.Count;

        if (errors == 0)
            Log.Info($"[AdventurerDB] Валидация имён: ok ({total} сущностей, " +
                     $"{names.FirstNamesMale?.Count ?? 0}+{names.FirstNamesFemale?.Count ?? 0} имён, " +
                     $"{names.Surnames?.Count ?? 0} фамилий)");
        else
            Log.Error($"[AdventurerDB] Валидация имён: {errors} ошибок");
    }
}