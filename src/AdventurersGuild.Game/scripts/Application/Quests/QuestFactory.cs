namespace AdventurersGuild.Application.Quests;

using System;
using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Core;
using AdventurersGuild.Core.Localization;
using AdventurersGuild.Data.Adventurer;
using AdventurersGuild.Data.Quest;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Quests;

public class QuestFactory
{
    private readonly Random rng;

    public QuestFactory(int? seed = null)
    {
        rng = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public QuestModel Create(GameTime now, int codeLevel, int partySize)
    {
        var templates = QuestDatabase.Templates.Values.ToList();
        if (templates.Count == 0) return null;

        var template = templates[rng.Next(templates.Count)];

        // 1. Считаем бюджет
        int baseBudget = QuestBalance.Active.BudgetPerLevel.TryGetValue(codeLevel, out int b) ? b : codeLevel * 10;
        int totalBudget = (int)(baseBudget * partySize * template.ComplexityMultiplier);

        // 2. Бросаем кубик на главного (r = random(1, totalBudget))
        int mainCost = rng.Next(1, totalBudget + 1);
        int ambientBudget = totalBudget - mainCost;

        var encounters = new List<QuestEncounterInstance>();

        // 3. Главное столкновение
        var mainEncounter = BuildMainEncounter(template, mainCost, codeLevel);
        if (mainEncounter != null) encounters.Add(mainEncounter);

        // 4. Амбиент (дополнительные встречи)
        encounters.AddRange(BuildAmbientEncounters(template.LocationId, ambientBudget, codeLevel));

        // 5. Собираем QuestModel
        string lang = Loc.Language;
        return new QuestModel
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = template.Name.Get(lang) ?? template.Id,
            Description = template.Description.Get(lang) ?? "",
            TemplateId = template.Id,
            LocationId = template.LocationId,
            CodeLevel = codeLevel,
            Encounters = encounters,
            CreatedAt = now,
            ExpiresAt = now + GameTime.FromDays(3 + rng.Next(5)),
            GoldReward = codeLevel * 50,
            GloryReward = codeLevel * 10,
            Status = QuestStatus.Available
        };
    }

    private QuestEncounterInstance BuildMainEncounter(QuestTemplateInfo template, int budget, int codeLevel)
    {
        var species = UnitDatabase.GetSpecies(template.MainEncounter.SpeciesId);
        if (species == null) return null;

        // Подбираем класс так, чтобы уровень не превышал codeLevel (правило "меньше-но-сильнее")
        var validClasses = species.Classes.Where(c => c.MinLvl <= codeLevel).ToList();
        if (validClasses.Count == 0) return null;

        // Берем самый сильный доступный класс
        var cls = validClasses.OrderByDescending(c => c.MaxLvl).First();

        // Уровень юнита — максимальный доступный в этом классе, но не выше уровня квеста
        int level = Math.Min(cls.MaxLvl, codeLevel);

        return new QuestEncounterInstance
        {
            SpeciesId = species.Id,
            RoleId = template.MainEncounter.RoleId,
            Level = level,
            IsMainEncounter = true,
            PreCheckSkillId = template.MainEncounter.PreCheck?.SkillId,
            PreCheckTargetStat = template.MainEncounter.PreCheck?.TargetStat
        };
    }

    private List<QuestEncounterInstance> BuildAmbientEncounters(string locationId, int budget, int codeLevel)
    {
        var result = new List<QuestEncounterInstance>();
        if (budget <= 0) return result;

        var habitat = QuestDatabase.Habitats.GetValueOrDefault(locationId);
        if (habitat == null) return result;

        // Простой цикл: пока есть бюджет, добавляем встречу
        while (budget > 0)
        {
            var role = habitat.Roles.Keys.ElementAt(rng.Next(habitat.Roles.Count));
            var speciesDict = habitat.Roles[role];
            var speciesId = speciesDict.Keys.ElementAt(rng.Next(speciesDict.Count));

            var species = UnitDatabase.GetSpecies(speciesId);
            if (species == null) break;

            int level = rng.Next(1, Math.Max(2, codeLevel));

            // Стоимость встречи (условно level * 1)
            int cost = level;
            if (cost > budget) cost = budget;

            result.Add(new QuestEncounterInstance
            {
                SpeciesId = speciesId,
                RoleId = role,
                Level = level,
                IsMainEncounter = false
            });

            budget -= cost;
        }

        return result;
    }
}