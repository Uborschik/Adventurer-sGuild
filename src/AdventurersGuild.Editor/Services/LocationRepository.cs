namespace AdventurersGuild.Editor.Services;

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AdventurersGuild.Core.Localization;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Domain.Quests;

public sealed class LocationRepository(DataLoader loader)
{
    private const string filePath = "Data/Quest/Locations.json";

    private readonly List<LocationInfo> items = [];
    private readonly DataLoader loader = loader;

    public IReadOnlyList<LocationInfo> All
    {
        get
        {
            EnsureLoaded();
            return items;
        }
    }

    public LocationInfo Get(string id)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(id)) return null;
        return items.FirstOrDefault(l => l.Id == id);
    }

    public LocationInfo NewDraft()
    {
        return new LocationInfo
        {
            Id = "",
            Name = new LocalizedNoun
            {
                Ru = new NounForms(),
                En = ""
            },
            DifficultyModifier = 0,
            TravelDaysModifier = 0,
        };
    }

    // Возвращает null при успехе, строку с ошибкой — при провале.
    public string Save(LocationInfo item, bool isNew)
    {
        EnsureLoaded();
        if (item == null) return "Пустой объект";

        if (string.IsNullOrWhiteSpace(item.Id))
            return "Id не может быть пустым";

        if (isNew && items.Any(l => l.Id == item.Id))
            return $"Локация с id '{item.Id}' уже существует";

        int idx = items.FindIndex(l => l.Id == item.Id);
        if (idx >= 0)
            items[idx] = item;
        else
            items.Add(item);

        return WriteAll();
    }

    public string Delete(string id)
    {
        EnsureLoaded();
        var item = Get(id);
        if (item == null) return $"Локация '{id}' не найдена";

        items.Remove(item);
        return WriteAll();
    }

    private void EnsureLoaded()
    {
        if (items.Count > 0) return;
        loader.EnsureLoaded();

        items.Clear();
        if (QuestDatabase.Locations != null)
            items.AddRange(QuestDatabase.Locations.Values);
    }

    private string WriteAll()
    {
        try
        {
            var db = new LocationDatabase { Locations = items.ToList() };
            JsonWriter.Write(filePath, db);
            return null;
        }
        catch (System.Exception ex)
        {
            return $"Ошибка записи: {ex.Message}";
        }
    }

    // Deep copy через JSON — для безопасного редактирования.
    public static LocationInfo Clone(LocationInfo src)
    {
        if (src == null) return null;

        var json = JsonSerializer.Serialize(src);
        return JsonSerializer.Deserialize<LocationInfo>(json, Options);
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };
}