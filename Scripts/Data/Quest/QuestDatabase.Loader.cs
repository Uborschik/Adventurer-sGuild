using System;
using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Core;
using AdventurersGuild.Data.Adventurer;
using AdventurersGuild.Data.Balance;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Quests;

namespace AdventurersGuild.Data.Quest;

public static partial class QuestDatabase
{
    private const string DataPath = "res://Resources/Data/Quest/";

    public static void Load()
    {
        Types = JsonLoader.Load<QuestTypeDatabase>(DataPath + "QuestTypes.json")
            .Types.ToDictionary(t => t.Id);

        LoadGrades();
        LoadPhases();
        LoadCreatures();
        LoadLocations();
        LoadTemplates();

        Validate();
        ValidateDeclensions();
    }

    private static void LoadGrades()
    {
        var gradeList = JsonLoader.Load<QuestGradeDatabase>(DataPath + "QuestGrades.json").Grades;
        Grades = gradeList;

        var byRole = new Dictionary<QuestGradeRole, QuestGradeInfo>();
        foreach (var grade in gradeList)
        {
            if (!Enum.TryParse<QuestGradeRole>(grade.Role, ignoreCase: true, out var role))
            {
                Log.Error($"[QuestDB] Неизвестная роль градации: '{grade.Role}'");
                continue;
            }
            if (byRole.ContainsKey(role))
            {
                Log.Error($"[QuestDB] Роль '{role}' дублируется");
                continue;
            }
            byRole[role] = grade;
        }

        foreach (QuestGradeRole role in Enum.GetValues<QuestGradeRole>())
            if (!byRole.ContainsKey(role))
                Log.Error($"[QuestDB] Нет градации с ролью {role}");

        GradesByRole = byRole;
    }

    private static void LoadPhases()
    {
        var db = JsonLoader.Load<QuestPhaseDatabase>(DataPath + "QuestPhases.json");
        Phases = db.Phases.ToDictionary(p => p.Id);
    }

    private static void LoadCreatures()
    {
        var list = JsonLoader.Load<CreatureDatabase>(DataPath + "Creatures.json").Creatures;
        Creatures = list.ToDictionary(c => c.Id);
    }

    private static void LoadLocations()
    {
        var list = JsonLoader.Load<LocationDatabase>(DataPath + "Locations.json").Locations;
        Locations = list.ToDictionary(l => l.Id);
    }

    private static void LoadTemplates()
    {
        Templates = JsonLoader.Load<QuestTemplateDatabase>(DataPath + "QuestTemplates.json").Templates;
    }

    public static List<QuestPhaseTemplate> ResolveQuest(QuestTemplate template, LocationInfo location, IReadOnlyList<CreatureInfo> creatures, int codeLevel)
    {
        if (template == null) return null;

        var ab = AdventurerBalance.Active;
        double refMaxCodeLevel = ab.StatStartPool + (codeLevel - 1) * ab.StatBaseSlope;

        // 1. Отфильтровать существ по допустимому уровню.
        var validCreatures = new List<CreatureInfo>();
        if (creatures != null)
        {
            foreach (var c in creatures)
            {
                if (c == null) continue;
                if (codeLevel >= c.MinLvl && codeLevel <= c.MaxLvl)
                    validCreatures.Add(c);
            }
        }

        // 2. Собрать обещания: phaseId → (creature, promise). Первое существо выигрывает.
        var fulfilled = new Dictionary<string, (CreatureInfo creature, PhasePromise promise)>();
        foreach (var creature in validCreatures)
        {
            if (creature.Promises == null) continue;
            foreach (var promise in creature.Promises)
            {
                if (string.IsNullOrEmpty(promise.Phase)) continue;
                if (!fulfilled.ContainsKey(promise.Phase))
                    fulfilled[promise.Phase] = (creature, promise);
            }
        }

        // 3. Проверить, что все required-фазы кем-то покрыты.
        var required = template.Requires ?? new List<string>();
        foreach (var req in required)
        {
            if (!fulfilled.ContainsKey(req))
            {
                Log.Error($"[QuestDB] ResolveQuest: шаблон '{template.Id}' требует '{req}', " +
                          $"но ни одно существо в квесте его не обещает. Квест не создаётся.");
                return null;
            }
        }

        var result = new List<QuestPhaseTemplate>();

        // 4. Сгенерировать фазы-маркеры из обещаний существ.
        foreach (var req in required)
        {
            if (!Phases.TryGetValue(req, out var phaseTemplate))
            {
                Log.Error($"[QuestDB] ResolveQuest: фаза '{req}' не найдена в QuestPhases.json");
                return null;
            }
            if (!phaseTemplate.IsMarker)
            {
                Log.Error($"[QuestDB] ResolveQuest: '{req}' в requires шаблона '{template.Id}', " +
                          $"но в QuestPhases.json не помечен как маркер");
                return null;
            }

            var (creature, promise) = fulfilled[req];
            var creatureStats = ComputeCreatureStats(creature, codeLevel, ab);

            var solutions = new List<PhaseSolution>();
            if (phaseTemplate.Paths != null)
            {
                foreach (var path in phaseTemplate.Paths)
                {
                    if (path.Checks == null || path.Checks.Count == 0) continue;

                    var sol = new PhaseSolution { Skills = new Dictionary<string, double>() };
                    foreach (var check in path.Checks)
                    {
                        if (string.IsNullOrEmpty(check.Skill)) continue;

                        double dc = ComputeCheckDc(check, creatureStats, refMaxCodeLevel);
                        if (dc >= 0)
                            sol.Skills[check.Skill] = dc;
                    }
                    if (sol.Skills.Count > 0) solutions.Add(sol);
                }
            }

            result.Add(new QuestPhaseTemplate
            {
                Id = phaseTemplate.Id,
                Name = phaseTemplate.Name,
                IsMarker = true,
                Critical = phaseTemplate.Critical,
                BaseDurationMinutes = promise.DurationMinutes,
                ExpReward = promise.ExpReward,
                Solutions = solutions,
                OnFail = phaseTemplate.OnFail,
            });
        }

        // 5. Добавить standalone-фазы из локации.
        if (location?.Standalone != null)
        {
            foreach (var standaloneId in location.Standalone)
            {
                if (!Phases.TryGetValue(standaloneId, out var standaloneTemplate))
                {
                    Log.Error($"[QuestDB] ResolveQuest: standalone '{standaloneId}' не найден");
                    continue;
                }
                if (standaloneTemplate.IsMarker)
                {
                    Log.Error($"[QuestDB] ResolveQuest: '{standaloneId}' в location.Standalone, " +
                              $"но помечен как маркер");
                    continue;
                }

                var clonedSolutions = new List<PhaseSolution>();
                if (standaloneTemplate.Solutions != null)
                {
                    foreach (var s in standaloneTemplate.Solutions)
                    {
                        clonedSolutions.Add(new PhaseSolution
                        {
                            Skills = s.Skills != null
                                ? new Dictionary<string, double>(s.Skills)
                                : new Dictionary<string, double>(),
                        });
                    }
                }

                result.Add(new QuestPhaseTemplate
                {
                    Id = standaloneTemplate.Id,
                    Name = standaloneTemplate.Name,
                    IsMarker = false,
                    Critical = standaloneTemplate.Critical,
                    BaseDurationMinutes = standaloneTemplate.BaseDurationMinutes,
                    ExpReward = standaloneTemplate.ExpReward,
                    Solutions = clonedSolutions,
                    OnFail = standaloneTemplate.OnFail,
                });
            }
        }

        // 6. Применить difficultyModifier шаблона и локации.
        double questDifficulty =
            template.DifficultyModifier + (location?.DifficultyModifier ?? 0);

        if (Math.Abs(questDifficulty) > 1e-9)
        {
            foreach (var phase in result)
            {
                foreach (var sol in phase.Solutions)
                {
                    var keys = new List<string>(sol.Skills.Keys);
                    foreach (var k in keys)
                        sol.Skills[k] += questDifficulty;
                }
            }
        }

        return result;
    }

    private static double ComputeCheckDc(PhaseCheck check, IReadOnlyDictionary<string, double> creatureStats, double refMaxCodeLevel)
    {
        // Нет resistance — использовать стат скилла напрямую (как раньше).
        if (check.Resistance == null || check.Resistance.Count == 0)
        {
            string statId = AdventurerDatabase.StatForSkill(check.Skill);
            if (statId == null) return -1;
            if (!creatureStats.TryGetValue(statId, out double v)) return -1;
            return v / refMaxCodeLevel;
        }

        // Иначе — среднее по resistance-статам.
        double sum = 0;
        int count = 0;
        foreach (var statId in check.Resistance)
        {
            if (creatureStats.TryGetValue(statId, out double v))
            {
                sum += v;
                count++;
            }
        }

        if (count == 0) return -1;
        return (sum / count) / refMaxCodeLevel;
    }

    private static Dictionary<string, double> ComputeCreatureStats(CreatureInfo creature, int codeLevel, AdventurerBalanceProfile ab)
    {
        int level = Math.Clamp(codeLevel, creature.MinLvl, creature.MaxLvl);
        double refMax = ab.StatStartPool + (level - 1) * ab.StatBaseSlope;

        var stats = new Dictionary<string, double>();
        foreach (var statId in StatIds.All)
        {
            double weight = 0.20;   // дефолт как в StatGrowth
            if (creature.GrowthWeights != null
                && creature.GrowthWeights.TryGetValue(statId, out var w))
                weight = w;

            stats[statId] = ab.StatBaseValue + refMax * weight;
        }
        return stats;
    }

    public static void ApplyData(IReadOnlyDictionary<string, QuestTypeInfo> types = null, IReadOnlyList<QuestGradeInfo> grades = null, IReadOnlyDictionary<string, QuestPhaseTemplate> phases = null, IReadOnlyDictionary<string, CreatureInfo> creatures = null, IReadOnlyDictionary<string, LocationInfo> locations = null, IReadOnlyList<QuestTemplate> templates = null)
    {
        if (types != null) Types = types;
        if (phases != null) Phases = phases;
        if (creatures != null) Creatures = creatures;
        if (locations != null) Locations = locations;
        if (templates != null) Templates = templates;

        if (grades != null)
        {
            Grades = grades;
            var byRole = new Dictionary<QuestGradeRole, QuestGradeInfo>();
            foreach (var g in grades)
            {
                if (!Enum.TryParse<QuestGradeRole>(g.Role, ignoreCase: true, out var role)) continue;
                if (byRole.ContainsKey(role)) continue;
                byRole[role] = g;
            }
            GradesByRole = byRole;
        }
    }
}