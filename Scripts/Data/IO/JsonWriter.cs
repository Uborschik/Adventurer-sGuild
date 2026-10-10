using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace AdventurersGuild.Data.IO;

public static class JsonWriter
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(),
        },
    };

    public static void Write<T>(string resPath, T data)
    {
        string json = JsonSerializer.Serialize(data, Opts);

        using var file = FileAccess.Open(resPath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            throw new InvalidOperationException(
                $"JsonWriter: не удалось открыть '{resPath}' для записи " +
                $"({FileAccess.GetOpenError()})");
        }

        file.StoreString(json);
        file.Close();
    }

    public static string ToJson<T>(T data) => JsonSerializer.Serialize(data, Opts);
}