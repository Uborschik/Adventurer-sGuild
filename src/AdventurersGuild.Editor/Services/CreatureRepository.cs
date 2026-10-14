namespace AdventurersGuild.Editor.Services;

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AdventurersGuild.Core.Localization;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Quests;

public sealed class CreatureRepository
{
    private const string FilePath = "Resources/Data/Quest/Creatures.json";

    private readonly List<CreatureInfo> items = new();
    private readonly DataLoader loader;

    public CreatureRepository(DataLoader loader)
    {
        this.loader = loader;
    }

    public IReadOnlyList<CreatureInfo> All
    {
        get { EnsureLoaded(); return items; }
    }

    public CreatureInfo Get(string id)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(id)) return null;
        return items.FirstOrDefault(c => c.Id == id);
    }

    public CreatureInfo NewDraft()
    {
        var item = new CreatureInfo
        {
            Id = "",
            Name = new LocalizedNoun { Ru = new NounForms(), En = "" },
            MinLvl = 1,
            MaxLvl = 60,
            GrowthWeights = [],
            Contributions = [],
        };

        Normalize(item);
        return item;
    }

    public string Save(CreatureInfo item)
    {
        EnsureLoaded();
        if (item == null) return "Пустой объект";

        var errors = Validate(item);
        if (errors.Count > 0) return string.Join("\n", errors);

        if (!items.Contains(item))
            items.Add(item);

        return WriteAll();
    }

    public string Delete(string id)
    {
        EnsureLoaded();
        var item = Get(id);
        if (item == null) return $"Существо '{id}' не найдено";

        items.Remove(item);
        return WriteAll();
    }

    public IReadOnlyList<string> Validate(CreatureInfo item)
    {
        var errors = new List<string>();
        if (item == null) { errors.Add("Пустой объект"); return errors; }

        if (string.IsNullOrWhiteSpace(item.Id))
            errors.Add("Id не может быть пустым");
        if (string.IsNullOrEmpty(item.Name?.Ru?.Nom))
            errors.Add("Не задано русское имя (именительный падеж)");
        if (string.IsNullOrEmpty(item.Name?.En))
            errors.Add("Не задано английское имя");

        if (item.MaxLvl < item.MinLvl)
            errors.Add("MaxLvl не может быть меньше MinLvl");

        if (item.GrowthWeights == null || item.GrowthWeights.Count == 0)
        {
            errors.Add("Не заданы веса роста");
        }
        else
        {
            foreach (var statId in StatIds.All)
            {
                if (!item.GrowthWeights.TryGetValue(statId, out var w))
                    errors.Add($"Не задан вес для стата '{statId}'");
                else if (w < 0)
                    errors.Add($"Отрицательный вес для стата '{statId}': {w}");
            }

            foreach (var kv in item.GrowthWeights)
                if (!StatIds.All.Contains(kv.Key))
                    errors.Add($"Неизвестный стат '{kv.Key}' в весах роста");
        }

        if (item.Contributions == null || item.Contributions.Count == 0)
        {
            errors.Add("Не задано ни одной contribution");
        }
        else
        {
            for (int i = 0; i < item.Contributions.Count; i++)
            {
                var c = item.Contributions[i];
                string prefix = $"Contribution #{i + 1}:";

                if (string.IsNullOrEmpty(c.PhaseId))
                {
                    errors.Add($"{prefix} не задана фаза");
                    continue;
                }

                if (!QuestDatabase.Phases.ContainsKey(c.PhaseId))
                {
                    errors.Add($"{prefix} фаза '{c.PhaseId}' не найдена");
                    continue;
                }

                var phase = QuestDatabase.Phases[c.PhaseId];

                if (c.Tags == null || c.Tags.Count == 0)
                {
                    errors.Add($"{prefix} не задано ни одного тега");
                    continue;
                }

                foreach (var tag in c.Tags)
                {
                    if (!QuestDatabase.Tags.ContainsKey(tag))
                        errors.Add($"{prefix} тег '{tag}' не найден");
                    else if (!phase.Tags.Contains(tag))
                        errors.Add($"{prefix} тег '{tag}' отсутствует у фазы '{c.PhaseId}'");
                }
            }
        }

        return errors;
    }

    public string Save(CreatureInfo item, bool isNew)
    {
        EnsureLoaded();
        if (item == null) return "Пустой объект";

        if (isNew && items.Any(c => c.Id == item.Id))
            return $"Существо с id '{item.Id}' уже существует";

        var errors = Validate(item);
        if (errors.Count > 0) return string.Join("\n", errors);

        int idx = items.FindIndex(c => c.Id == item.Id);
        if (idx >= 0)
            items[idx] = item;
        else
            items.Add(item);

        return WriteAll();
    }

    private void EnsureLoaded()
    {
        if (items.Count > 0) return;
        loader.EnsureLoaded();

        items.Clear();
        if (QuestDatabase.Creatures != null)
            items.AddRange(QuestDatabase.Creatures.Values);
    }

    private string WriteAll()
    {
        try
        {
            var db = new CreatureDatabase { Creatures = items.ToList() };
            JsonWriter.Write(FilePath, db);
            return null;
        }
        catch (System.Exception ex)
        {
            return $"Ошибка записи: {ex.Message}";
        }
    }

    public static CreatureInfo Clone(CreatureInfo src)
    {
        if (src == null) return null;

        var json = JsonSerializer.Serialize(src);
        var clone = JsonSerializer.Deserialize<CreatureInfo>(json, Options);
        Normalize(clone);
        return clone;
    }

    private static void Normalize(CreatureInfo c)
    {
        if (c == null) return;

        c.Name ??= new LocalizedNoun();
        c.Name.Ru ??= new NounForms();
        c.Name.En ??= "";

        c.GrowthWeights ??= new Dictionary<string, double>();
        foreach (var statId in StatIds.All)
            if (!c.GrowthWeights.ContainsKey(statId))
                c.GrowthWeights[statId] = 0;

        c.Contributions ??= [];
        foreach (var contrib in c.Contributions)
            contrib.Tags ??= [];
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };
}