using System;
using System.Collections.Generic;
using System.Linq;

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
        var templateList = JsonLoader.Load<QuestTemplateDatabase>(DataPath + "QuestTemplates.json").Templates;
        Templates = templateList;
        RebuildResolved();
    }

    private static void RebuildResolved()
    {
        resolvedCombos = new Dictionary<(string, string), ResolvedTemplate>();

        foreach (var t in Templates)
        {
            if (t.Creatures == null || t.Creatures.Count == 0)
            {
                var resolved = ResolveTemplate(t, null);
                if (resolved != null)
                    resolvedCombos[(t.Id, "")] = resolved;
                continue;
            }

            foreach (var creatureId in t.Creatures)
            {
                var resolved = ResolveTemplate(t, creatureId);
                if (resolved != null)
                    resolvedCombos[(t.Id, creatureId)] = resolved;
            }
        }
    }

    /// <summary>
    /// Установить активные данные квестов в память, не читая с диска.
    /// Применяется редакторным тулингом для preview-режима.
    /// 
    /// Валидация НЕ вызывается — preview может содержать промежуточные состояния.
    /// resolvedCombos пересобирается из переданных Templates.
    /// 
    /// Параметры, оставленные null, сохраняют текущее значение соответствующей коллекции.
    /// </summary>
    public static void ApplyData(
        IReadOnlyDictionary<string, QuestTypeInfo> types = null,
        IReadOnlyList<QuestGradeInfo> grades = null,
        IReadOnlyDictionary<string, QuestPhaseTemplate> phases = null,
        IReadOnlyDictionary<string, CreatureInfo> creatures = null,
        IReadOnlyDictionary<string, LocationInfo> locations = null,
        IReadOnlyList<QuestTemplate> templates = null)
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
                if (!System.Enum.TryParse<QuestGradeRole>(g.Role, ignoreCase: true, out var role))
                {
                    Log.Error($"[QuestDB] Неизвестная роль градации: '{g.Role}'");
                    continue;
                }
                if (byRole.ContainsKey(role))
                {
                    Log.Error($"[QuestDB] Роль '{role}' дублируется");
                    continue;
                }
                byRole[role] = g;
            }

            GradesByRole = byRole;
        }

        // resolvedCombos пересобираем только если поменялись templates или creatures.
        // Если передана только часть данных (например, только phases в PhasesTab),
        // resolvedCombos собирается заново из ТЕКУЩИХ Templates + ТЕКУЩИХ Creatures,
        // что уже учитывает изменения phases.
        if (templates != null || creatures != null || phases != null)
            RebuildResolved();
    }
}