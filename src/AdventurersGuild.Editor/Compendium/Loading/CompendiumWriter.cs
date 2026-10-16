#nullable enable

namespace AdventurersGuild.Editor.Compendium.Loading;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using AdventurersGuild.Data.IO;

public sealed class CompendiumWriter
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public void SaveFile(CompendiumIndex index, string kind)
    {
        var info = Kinds.Get(kind)
            ?? throw new InvalidOperationException($"Kind '{kind}' не зарегистрирован.");

        var root = index.GetRoot(kind)
            ?? throw new InvalidOperationException($"Корень '{kind}' не загружен.");

        string json = root.ToJsonString(WriteOptions);
        FileSystem.Current.WriteAllText(info.File, json);
    }

    public void SaveNew(CompendiumIndex index, CompendiumEntry entry)
    {
        var info = Kinds.Get(entry.Kind)
            ?? throw new InvalidOperationException($"Kind '{entry.Kind}' не зарегистрирован.");

        var root = index.GetRoot(entry.Kind)
            ?? throw new InvalidOperationException($"Корень '{entry.Kind}' не загружен.");

        if (root[info.RootPath] is not JsonArray array)
            throw new InvalidOperationException($"В файле '{info.File}' нет массива '{info.RootPath}'.");

        array.Add(entry.Json);
        index.Add(entry);
        entry.RefreshSnapshot();
        SaveFile(index, entry.Kind);
    }

    public void Delete(CompendiumIndex index, CompendiumEntry entry)
    {
        var info = Kinds.Get(entry.Kind)
            ?? throw new InvalidOperationException($"Kind '{entry.Kind}' не зарегистрирован.");

        var root = index.GetRoot(entry.Kind)
            ?? throw new InvalidOperationException($"Корень '{entry.Kind}' не загружен.");

        if (root[info.RootPath] is not JsonArray array) return;

        int idx = -1;
        for (int i = 0; i < array.Count; i++)
        {
            if (ReferenceEquals(array[i], entry.Json)) { idx = i; break; }
        }

        if (idx >= 0) array.RemoveAt(idx);
        SaveFile(index, entry.Kind);
    }

    public void Save(CompendiumIndex index, CompendiumEntry entry)
    {
        entry.RefreshSnapshot();
        SaveFile(index, entry.Kind);
    }
}