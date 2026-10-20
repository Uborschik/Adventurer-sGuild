namespace AdventurersGuild.Application.Quests;

using System;
using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Core;
using AdventurersGuild.Domain.Quests;
using AdventurersGuild.Domain.Units;

public class QuestFactory(int? seed = null)
{
    private readonly Random rng = seed.HasValue ? new Random(seed.Value) : new Random();

    public QuestModel Create(GameTime now, int codeLevel, int partySize)
    {
        var templates = QuestDatabase.Templates.Values.ToList();
        if (templates.Count == 0) return null;

        var template = templates[rng.Next(templates.Count)];

        // 1. Считаем общий бюджет
        int baseBudget = QuestBalance.Active.BudgetPerLevel.TryGetValue(codeLevel, out int b) ? b : codeLevel * 10;
        int totalBudget = (int)(baseBudget * partySize * template.ComplexityMultiplier);

        // 2. Главное столкновение забирает от 50% до 70% бюджета
        double mainShare = 0.5 + (rng.NextDouble() * 0.2);
        int mainBudget = (int)(totalBudget * mainShare);

        var encounters = new List<QuestEncounterInstance>();

        // 3. Строим главное столкновение
        var mainEncounter = BuildMainEncounter(template.MainEncounter, mainBudget, codeLevel);
        if (mainEncounter != null)
        {
            encounters.Add(mainEncounter);
            // Вычитаем реальную стоимость главного врага из общего бюджета
            int actualMainCost = CalculateCost(mainEncounter.SpeciesId, mainEncounter.Level);
            totalBudget -= actualMainCost;
        }

        // 4. Строим амбиент (дополнительные встречи) на остаток
        encounters.AddRange(BuildAmbientEncounters(template, totalBudget, codeLevel));

        // 5. Перемешиваем амбиент, но главного оставляем в конце (кульминация)
        var ambientOnly = encounters.Where(e => !e.IsMainEncounter).ToList();
        var mainOnly = encounters.Where(e => e.IsMainEncounter).ToList();

        // Простое перемешивание (Fisher-Yates)
        for (int i = ambientOnly.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            var temp = ambientOnly[i];
            ambientOnly[i] = ambientOnly[j];
            ambientOnly[j] = temp;
        }

        var finalEncounters = ambientOnly.Concat(mainOnly).ToList();

        // 6. Собираем QuestModel
        string lang = Loc.Language;
        return new QuestModel
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = template.Name.Get(lang) ?? template.Id,
            Description = template.Description.Get(lang) ?? "",
            TemplateId = template.Id,
            LocationId = template.LocationId,
            CodeLevel = codeLevel,
            Encounters = finalEncounters,
            CreatedAt = now,
            ExpiresAt = now + GameTime.FromDays(3 + rng.Next(5)),
            GoldReward = codeLevel * 50,
            GloryReward = codeLevel * 10,
            Status = QuestStatus.Available
        };
    }

    private QuestEncounterInstance BuildMainEncounter(MainEncounterInfo info, int budget, int codeLevel)
    {
        if (info == null) return null;

        var species = UnitDatabase.GetSpecies(info.SpeciesId);
        if (species == null) return null;

        // Ищем класс и уровень, которые влезут в бюджет (правило "меньше-но-сильнее")
        int bestLevel = 1;
        int bestCost = -1;
        string bestClassId = null;

        foreach (var sc in species.Classes)
        {
            if (sc.MinLvl > codeLevel) continue; // Уровень класса не должен превышать уровень квеста

            var cls = UnitDatabase.GetClass(sc.ClassId);
            int classCost = cls?.ClassCost ?? 1;

            // Максимальный уровень, который мы можем себе позволить на этот бюджет
            int maxAffordableLevel = budget / classCost;

            // Ограничиваем уровнем квеста и уровнем самого класса
            int targetLevel = Math.Min(maxAffordableLevel, Math.Min(sc.MaxLvl, codeLevel));

            if (targetLevel >= sc.MinLvl)
            {
                int actualCost = targetLevel * classCost;
                if (actualCost > bestCost)
                {
                    bestCost = actualCost;
                    bestLevel = targetLevel;
                    bestClassId = sc.ClassId;
                }
            }
        }

        return new QuestEncounterInstance
        {
            SpeciesId = info.SpeciesId,
            ClassId = bestClassId,
            RoleId = info.RoleId,
            Level = bestLevel,
            IsMainEncounter = true,
            PreCheckSkillId = info.PreCheck?.SkillId,
            PreCheckTargetStat = info.PreCheck?.TargetStat
        };
    }

    private List<QuestEncounterInstance> BuildAmbientEncounters(QuestTemplateInfo template, int budget, int codeLevel)
    {
        var result = new List<QuestEncounterInstance>();
        if (budget <= 0) return result;

        var habitat = QuestDatabase.Habitats.GetValueOrDefault(template.LocationId);
        if (habitat == null) return result;

        // 1. Считаем, сколько боевых слотов выпало
        int combatCount = rng.Next(template.AmbientEncounters.Combat.Min, template.AmbientEncounters.Combat.Max + 1);

        // 2. Делим бюджет на эти слоты
        if (combatCount > 0)
        {
            int budgetPerCombat = budget / combatCount;
            for (int i = 0; i < combatCount; i++)
            {
                // Ищем боевую роль в бестиарии (predator, berserker)
                var combatRoles = habitat.Roles.Keys.Where(r => r == "predator" || r == "berserker").ToList();
                if (combatRoles.Count == 0) break;

                var role = combatRoles[rng.Next(combatRoles.Count)];
                var speciesDict = habitat.Roles[role];
                var speciesId = speciesDict.Keys.ElementAt(rng.Next(speciesDict.Count));

                // Подбираем уровень под бюджет слота
                var encounter = CreateEncounterForBudget(speciesId, role, budgetPerCombat, codeLevel, false);
                if (encounter != null) result.Add(encounter);
            }
        }

        // 3. Добавляем социальные встречи (они не тратят бюджет, как мы и договаривались)
        int socialCount = rng.Next(template.AmbientEncounters.Social.Min, template.AmbientEncounters.Social.Max + 1);
        for (int i = 0; i < socialCount; i++)
        {
            var socialRoles = habitat.Roles.Keys.Where(r => r == "pilgrim" || r == "merchant" || r == "guard").ToList();
            if (socialRoles.Count == 0) break;

            var role = socialRoles[rng.Next(socialRoles.Count)];
            var speciesDict = habitat.Roles[role];
            var speciesId = speciesDict.Keys.ElementAt(rng.Next(speciesDict.Count));

            // Социальные встречи генерируем на 1-3 уровня ниже квеста
            int socialLevel = Math.Max(1, codeLevel - rng.Next(1, 4));
            result.Add(new QuestEncounterInstance
            {
                SpeciesId = speciesId,
                RoleId = role,
                Level = socialLevel,
                IsMainEncounter = false
            });
        }

        return result;
    }

    private QuestEncounterInstance CreateEncounterForBudget(string speciesId, string roleId, int budget, int codeLevel, bool isMain)
    {
        var species = UnitDatabase.GetSpecies(speciesId);
        if (species == null) return null;

        int bestLevel = 1;
        int bestCost = -1;
        string bestClassId = null;

        foreach (var sc in species.Classes)
        {
            if (sc.MinLvl > codeLevel) continue;

            var cls = UnitDatabase.GetClass(sc.ClassId);
            int classCost = cls?.ClassCost ?? 1;

            int maxAffordableLevel = budget / classCost;
            int targetLevel = Math.Min(maxAffordableLevel, Math.Min(sc.MaxLvl, codeLevel));

            if (targetLevel >= sc.MinLvl)
            {
                int actualCost = targetLevel * classCost;
                if (actualCost > bestCost)
                {
                    bestCost = actualCost;
                    bestLevel = targetLevel;
                    bestClassId = sc.ClassId;
                }
            }
        }

        return new QuestEncounterInstance
        {
            SpeciesId = speciesId,
            ClassId = bestClassId,
            RoleId = roleId,
            Level = bestLevel,
            IsMainEncounter = isMain
        };
    }

    private int CalculateCost(string speciesId, int level)
    {
        // Условная стоимость (можно уточнить в зависимости от класса)
        var species = UnitDatabase.GetSpecies(speciesId);
        if (species == null) return level;

        // В реальности тут нужно найти класс, но для простоты берем 1 или 2
        return level;
    }
}