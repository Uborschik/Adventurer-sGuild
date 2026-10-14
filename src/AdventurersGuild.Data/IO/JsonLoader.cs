namespace AdventurersGuild.Data.IO;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    public static T Load<T>(string path) where T : class
    {
        FileSystem.EnsureReady();

        string json = FileSystem.Current.ReadAllText(path);
        if (string.IsNullOrEmpty(json))
            throw new InvalidOperationException($"Не удалось прочитать {path}");

        var result = JsonSerializer.Deserialize<T>(json, Opts);
        if (result == null)
            throw new InvalidOperationException($"Не удалось распарсить {path}");

        return result;
    }
}