namespace AdventurersGuild.Data.IO;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    public static void Write<T>(string path, T data)
    {
        FileSystem.EnsureReady();

        string json = JsonSerializer.Serialize(data, Opts);
        FileSystem.Current.WriteAllText(path, json);
    }

    public static string ToJson<T>(T data) => JsonSerializer.Serialize(data, Opts);
}