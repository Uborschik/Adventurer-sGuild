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

        int baseBudget = QuestBalance.Active.BudgetPerLevel.TryGetValue(codeLevel, out int b) ? b : codeLevel * 10;
        int totalBudget = (int)(baseBudget * partySize * template.ComplexityMultiplier);

        double mainShare = 0.5 + (rng.NextDouble() * 0.2);
        int mainBudget = (int)(totalBudget * mainShare);

        var encounters = new List<QuestEncounterInstance>();

        var mainEncounter = BuildMainEncounter(template.MainEncounter, mainBudget, codeLevel);
        if (mainEncounter != null)
        {
            encounters.Add(mainEncounter);
            int actualMainCost = mainEncounter.Units.Sum(u => CalculateUnitCost(u.ClassId, u.Level));
            totalBudget -= actualMainCost;
        }

        encounters.AddRange(BuildAmbientEncounters(template, totalBudget, codeLevel));

        var ambientOnly = encounters.Where(e => !e.IsMainEncounter).ToList();
        var mainOnly = encounters.Where(e => e.IsMainEncounter).ToList();

        for (int i = ambientOnly.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (ambientOnly[j], ambientOnly[i]) = (ambientOnly[i], ambientOnly[j]);
        }

        var finalEncounters = ambientOnly.Concat(mainOnly).ToList();

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

    private static QuestEncounterInstance BuildMainEncounter(MainEncounterInfo info, int budget, int codeLevel)
    {
        if (info == null) return null;
        var species = UnitDatabase.GetSpecies(info.SpeciesId);
        if (species == null) return null;

        // Пока делаем 1 главного врага. (Позже можно генерировать группу: 1 босс + свита)
        var bestUnit = FindBestUnitForBudget(species, budget, codeLevel, info.RoleId);
        if (bestUnit == null) return null;

        return new QuestEncounterInstance
        {
            Units = [bestUnit],
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

        int combatCount = rng.Next(template.AmbientEncounters.Combat.Min, template.AmbientEncounters.Combat.Max + 1);

        if (combatCount > 0)
        {
            int budgetPerCombat = budget / combatCount;

            for (int i = 0; i < combatCount; i++)
            {
                var combatRoles = habitat.Roles.Keys
                    .Where(rId => QuestDatabase.Roles.TryGetValue(rId, out var role) && role.Type == EncounterType.Combat)
                    .ToList();

                if (combatRoles.Count == 0) break;

                var role = combatRoles[rng.Next(combatRoles.Count)];
                var speciesDict = habitat.Roles[role];
                var speciesId = speciesDict.Keys.ElementAt(rng.Next(speciesDict.Count));

                var species = UnitDatabase.GetSpecies(speciesId);

                if (species == null) continue;

                var unit = FindBestUnitForBudget(species, budgetPerCombat, codeLevel, role);

                if (unit != null)
                {
                    result.Add(new QuestEncounterInstance
                    {
                        Units = [unit],
                        IsMainEncounter = false
                    });
                }
            }
        }

        int socialCount = rng.Next(template.AmbientEncounters.Social.Min, template.AmbientEncounters.Social.Max + 1);

        for (int i = 0; i < socialCount; i++)
        {
            var socialRoles = habitat.Roles.Keys
                .Where(rId => QuestDatabase.Roles.TryGetValue(rId, out var role) && role.Type == EncounterType.Social)
                .ToList();

            if (socialRoles.Count == 0) break;

            var role = socialRoles[rng.Next(socialRoles.Count)];
            var speciesDict = habitat.Roles[role];
            var speciesId = speciesDict.Keys.ElementAt(rng.Next(speciesDict.Count));

            int socialLevel = Math.Max(1, codeLevel - rng.Next(1, 4));

            result.Add(new QuestEncounterInstance
            {
                Units =
                [
                    new(speciesId, null, role, socialLevel)
                ],
                IsMainEncounter = false
            });
        }

        return result;
    }

    private static EncounterUnit FindBestUnitForBudget(SpeciesInfo species, int budget, int codeLevel, string roleId)
    {
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

        if (bestClassId == null) return null;
        return new EncounterUnit(species.Id, bestClassId, roleId, bestLevel);
    }

    private static int CalculateUnitCost(string classId, int level)
    {
        var cls = UnitDatabase.GetClass(classId);
        return level * (cls?.ClassCost ?? 1);
    }
}