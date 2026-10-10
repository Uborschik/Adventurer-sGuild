using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace AdventurersGuild.Data.IO;

public static class JsonLoader
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
    {
        new JsonStringEnumConverter(),
        new IntFromFloatConverter()
    }
    };

    public static T Load<T>(string resPath) where T : class
    {
        string json = FileAccess.GetFileAsString(resPath);
        if (string.IsNullOrEmpty(json))
            throw new InvalidOperationException($"Не удалось прочитать {resPath}");

        var result = JsonSerializer.Deserialize<T>(json, Opts);
        if (result == null)
            throw new InvalidOperationException($"Не удалось распарсить {resPath}");

        return result;
    }
}