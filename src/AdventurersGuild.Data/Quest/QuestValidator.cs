namespace AdventurersGuild.Data.Quest;

using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Core;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Quests;

public static class QuestValidator
{
    public static void Validate()
    {
        int errors = 0;

        errors += ValidateInstructions();
        errors += ValidatePhases();
        errors += ValidateRoles();
        errors += ValidateBestiary();
        errors += ValidateTemplates();

        if (errors == 0)
            Log.Info($"[QuestDB] Валидация: ok " +
                     $"({QuestDatabase.Roles.Count} ролей, " +
                     $"{QuestDatabase.Templates.Count} шаблонов, " +
                     $"{QuestDatabase.Habitats.Count} локаций)");
        else
            Log.Error($"[QuestDB] Валидация: {errors} ошибок");
    }

    // === Instructions ===

    private static int ValidateInstructions()
    {
        int errors = 0;
        if (QuestDatabase.Instructions == null || QuestDatabase.Instructions.Count == 0)
        {
            Log.Error("[QuestDB] Инструкции не загружены");
            return 1;
        }

        foreach (var i in QuestDatabase.Instructions.Values)
        {
            if (string.IsNullOrEmpty(i.Id))
            {
                Log.Error("[QuestDB] Инструкция без id");
                errors++;
            }
        }
        return errors;
    }

    // === Phases ===

    private static int ValidatePhases()
    {
        int errors = 0;
        if (QuestDatabase.Phases == null || QuestDatabase.Phases.Count == 0)
        {
            Log.Error("[QuestDB] Фазы не загружены");
            return 1;
        }

        foreach (var p in QuestDatabase.Phases.Values)
        {
            if (string.IsNullOrEmpty(p.Id))
            {
                Log.Error("[QuestDB] Фаза без id");
                errors++;
            }
            if (string.IsNullOrEmpty(p.Name.Ru) || string.IsNullOrEmpty(p.Name.En))
            {
                Log.Error($"[QuestDB] Фаза '{p.Id}': неполное name");
                errors++;
            }
        }
        return errors;
    }

    // === Roles ===

    private static int ValidateRoles()
    {
        int errors = 0;
        if (QuestDatabase.Roles == null || QuestDatabase.Roles.Count == 0)
        {
            Log.Error("[QuestDB] Роли не загружены");
            return 1;
        }

        foreach (var role in QuestDatabase.Roles.Values)
        {
            if (role.Actions == null || role.Actions.Count == 0)
            {
                Log.Error($"[QuestDB] Роль '{role.Id}': пустой список actions");
                errors++;
                continue;
            }

            var actionIds = role.Actions.Select(a => a.Id).ToHashSet();

            foreach (var action in role.Actions)
            {
                // Проверка фазы
                if (!QuestDatabase.Phases.ContainsKey(action.PhaseId))
                {
                    Log.Error($"[QuestDB] Роль '{role.Id}', action '{action.Id}': неизвестная фаза '{action.PhaseId}'");
                    errors++;
                }

                // Проверка Check
                if (action.Check != null)
                {
                    var check = action.Check.Value; // Достаем структуру один раз

                    if (!StatIds.All.Contains(check.ActorStat))
                    {
                        Log.Error($"[QuestDB] Роль '{role.Id}', action '{action.Id}': неизвестный actorStat '{check.ActorStat}'");
                        errors++;
                    }

                    if (!string.IsNullOrEmpty(check.TargetSkill) && !UnitDatabase.IsValidSkill(check.TargetSkill))
                    {
                        Log.Error($"[QuestDB] Роль '{role.Id}', action '{action.Id}': неизвестный targetSkill '{check.TargetSkill}'");
                        errors++;
                    }

                    if (!string.IsNullOrEmpty(check.TargetStat) && !StatIds.All.Contains(check.TargetStat))
                    {
                        Log.Error($"[QuestDB] Роль '{role.Id}', action '{action.Id}': неизвестный targetStat '{check.TargetStat}'");
                        errors++;
                    }
                }

                // Проверка исходов (Success/Fail)
                errors += ValidateOutcome(role.Id, action.Id, action.Success, actionIds);
                errors += ValidateOutcome(role.Id, action.Id, action.Fail, actionIds);
            }
        }
        return errors;
    }

    private static int ValidateOutcome(string roleId, string actionId, OutcomeInfo outcome, HashSet<string> actionIds)
    {
        if (outcome == null || string.IsNullOrEmpty(outcome.Id)) return 0; // Если исхода нет (маловероятно)

        // Исход может быть инструкцией ("end", "combat") или другим action в этой же роли
        bool isInstruction = QuestDatabase.Instructions.ContainsKey(outcome.Id);
        bool isAction = actionIds.Contains(outcome.Id);

        if (!isInstruction && !isAction)
        {
            Log.Error($"[QuestDB] Роль '{roleId}', action '{actionId}': исход '{outcome.Id}' не является ни инструкцией, ни action в этой роли");
            return 1;
        }

        int errors = 0;
        if (outcome.Consequences != null)
        {
            foreach (var cons in outcome.Consequences)
            {
                if (cons.Id != "wound" && cons.Id != "death" && cons.Id != "delay" && cons.Id != "dc-penalty")
                {
                    Log.Error($"[QuestDB] Роль '{roleId}', action '{actionId}': неизвестное последствие '{cons.Id}'");
                    errors++;
                }
            }
        }
        return errors;
    }

    // === Bestiary ===

    private static int ValidateBestiary()
    {
        int errors = 0;
        if (QuestDatabase.Habitats == null || QuestDatabase.Habitats.Count == 0)
        {
            Log.Warn("[QuestDB] Бестиарий пуст");
            return 0;
        }

        foreach (var habitat in QuestDatabase.Habitats.Values)
        {
            foreach (var rolePair in habitat.Roles)
            {
                string roleId = rolePair.Key;
                if (!QuestDatabase.Roles.ContainsKey(roleId))
                {
                    Log.Error($"[QuestDB] Бестиарий '{habitat.LocationId}': неизвестная роль '{roleId}'");
                    errors++;
                }

                foreach (var speciesPair in rolePair.Value)
                {
                    string speciesId = speciesPair.Key;
                    if (!UnitDatabase.Species.ContainsKey(speciesId))
                    {
                        Log.Error($"[QuestDB] Бестиарий '{habitat.LocationId}': неизвестный вид '{speciesId}'");
                        errors++;
                    }
                }
            }
        }
        return errors;
    }

    // === Templates ===

    private static int ValidateTemplates()
    {
        int errors = 0;
        if (QuestDatabase.Templates == null || QuestDatabase.Templates.Count == 0)
        {
            Log.Error("[QuestDB] Шаблоны квестов не загружены");
            return 1;
        }

        foreach (var template in QuestDatabase.Templates.Values)
        {
            // Главное столкновение
            if (template.MainEncounter != null)
            {
                if (!UnitDatabase.Species.ContainsKey(template.MainEncounter.SpeciesId))
                {
                    Log.Error($"[QuestDB] Шаблон '{template.Id}': неизвестный вид '{template.MainEncounter.SpeciesId}' в mainEncounter");
                    errors++;
                }

                if (!QuestDatabase.Roles.ContainsKey(template.MainEncounter.RoleId))
                {
                    Log.Error($"[QuestDB] Шаблон '{template.Id}': неизвестная роль '{template.MainEncounter.RoleId}' в mainEncounter");
                    errors++;
                }

                if (template.MainEncounter.PreCheck != null)
                {
                    var pc = template.MainEncounter.PreCheck.Value;
                    if (!UnitDatabase.IsValidSkill(pc.SkillId))
                    {
                        Log.Error($"[QuestDB] Шаблон '{template.Id}': неизвестный навык '{pc.SkillId}' в preCheck");
                        errors++;
                    }
                    if (!StatIds.All.Contains(pc.TargetStat))
                    {
                        Log.Error($"[QuestDB] Шаблон '{template.Id}': неизвестный стат '{pc.TargetStat}' в preCheck");
                        errors++;
                    }
                }
            }
            else
            {
                Log.Error($"[QuestDB] Шаблон '{template.Id}': отсутствует mainEncounter");
                errors++;
            }

            // Проверка локации (если она есть в базе)
            // if (!QuestDatabase.Locations.ContainsKey(template.LocationId)) ...
        }
        return errors;
    }
}