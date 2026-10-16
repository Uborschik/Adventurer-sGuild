namespace AdventurersGuild.Editor.Compendium.Loading;

using System.Text.Json;
using System.Text.Json.Nodes;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Editor.Compendium.Schema;

public sealed class CompendiumJsonLoader
{
    private readonly SchemaRegistry schemas;

    public CompendiumJsonLoader(SchemaRegistry schemas)
    {
        this.schemas = schemas;
    }

    public CompendiumIndex LoadAll()
    {
        var index = new CompendiumIndex();
        foreach (var schema in schemas.All)
            LoadKind(schema, index);
        return index;
    }

    public void LoadKind(EntitySchema schema, CompendiumIndex index)
    {
        if (string.IsNullOrEmpty(schema.File) || string.IsNullOrEmpty(schema.RootPath)) return;

        string json = FileSystem.Current.ReadAllText(schema.File);

        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException($"Файл '{schema.File}' пуст или не читается.");

        var root = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });

        if (root is not JsonObject obj)
            throw new InvalidOperationException($"Корень файла '{schema.File}' должен быть объектом.");

        if (obj[schema.RootPath] is not JsonArray array)
            throw new InvalidOperationException($"В файле '{schema.File}' нет массива '{schema.RootPath}'.");

        index.SetRoot(schema.Kind, obj);

        foreach (var item in array)
        {
            if (item is not JsonObject itemObj) continue;

            string id = itemObj[schema.IdField]?.GetValue<string>() ?? "";
            if (string.IsNullOrEmpty(id)) continue;

            var entry = new CompendiumEntry
            {
                Kind = schema.Kind,
                Id = id,
                Json = itemObj,
                DisplayName = ExtractDisplayName(itemObj, schema),
            };
            entry.SetInitialSnapshot();
            index.Add(entry);
        }
    }

    private static string ExtractDisplayName(JsonObject obj, EntitySchema schema)
    {
        JsonNode? node = obj[schema.DisplayField];

        if (node is JsonValue val)
            return val.GetValue<string>() ?? obj[schema.IdField]?.GetValue<string>() ?? "?";

        if (node is JsonObject loc)
        {
            JsonNode? ru = loc["ru"];

            if (ru is JsonValue rv) return rv.GetValue<string>() ?? "?";
            if (ru is JsonObject ro) return ro["nom"]?.GetValue<string>() ?? "?";

            JsonNode? en = loc["en"];

            if (en is JsonValue ev) return ev.GetValue<string>() ?? "?";
        }

        return obj[schema.IdField]?.GetValue<string>() ?? "?";
    }
}