namespace AdventurersGuild.Editor.Compendium.Loading;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Editor.Compendium.Schema;

public sealed class CompendiumWriter
{
    private readonly SchemaRegistry _schemas;

    public CompendiumWriter(SchemaRegistry schemas)
    {
        _schemas = schemas;
    }

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Сохраняет весь файл kind'а обратно на диск. Все изменения entry.Json
    /// уже в дереве, поэтому достаточно сериализовать корень.
    /// </summary>
    public void SaveFile(CompendiumIndex index, string kind)
    {
        var schema = _schemas.Get(kind);
        var root = index.GetRoot(kind)
            ?? throw new InvalidOperationException(
                $"Корень файла для kind '{kind}' не загружен в индекс.");

        string json = root.ToJsonString(WriteOptions);
        FileSystem.Current.WriteAllText(schema.File, json);
    }

    /// <summary>
    /// Добавляет новый entry в файл и сохраняет.
    /// </summary>
    public void SaveNew(CompendiumIndex index, CompendiumEntry entry)
    {
        var schema = _schemas.Get(entry.Kind);

        if (string.IsNullOrEmpty(schema.RootPath))
            return;

        var root = index.GetRoot(entry.Kind)
            ?? throw new InvalidOperationException(
                $"Корень файла для kind '{entry.Kind}' не загружен.");

        if (root[schema.RootPath] is not JsonArray array)
            throw new InvalidOperationException(
                $"В файле '{schema.File}' нет массива '{schema.RootPath}'.");

        array.Add(entry.Json);
        index.Add(entry);
        entry.RefreshSnapshot();

        SaveFile(index, entry.Kind);
    }

    /// <summary>
    /// Удаляет entry из файла и сохраняет.
    /// </summary>
    public void Delete(CompendiumIndex index, CompendiumEntry entry)
    {
        var schema = _schemas.Get(entry.Kind);

        if (string.IsNullOrEmpty(schema.RootPath))
            return;

        var root = index.GetRoot(entry.Kind)
            ?? throw new InvalidOperationException(
                $"Корень файла для kind '{entry.Kind}' не загружен.");

        if (root[schema.RootPath] is not JsonArray array)
            throw new InvalidOperationException(
                $"В файле '{schema.File}' нет массива '{schema.RootPath}'.");

        // Найти индекс по id (сравниваем JsonObject по ссылке)
        int idx = -1;
        for (int i = 0; i < array.Count; i++)
        {
            if (ReferenceEquals(array[i], entry.Json))
            {
                idx = i;
                break;
            }
        }

        if (idx >= 0)
            array.RemoveAt(idx);

        SaveFile(index, entry.Kind);
    }

    /// <summary>
    /// Сохраняет уже существующий (или изменённый) entry.
    /// </summary>
    public void Save(CompendiumIndex index, CompendiumEntry entry)
    {
        entry.RefreshSnapshot();
        SaveFile(index, entry.Kind);
    }
}