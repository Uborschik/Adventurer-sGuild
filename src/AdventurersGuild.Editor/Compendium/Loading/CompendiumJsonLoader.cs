#nullable enable

namespace AdventurersGuild.Editor.Compendium.Loading;

using System.Text.Json;
using System.Text.Json.Nodes;
using AdventurersGuild.Data.IO;

public sealed class CompendiumJsonLoader
{
    public CompendiumIndex LoadAll()
    {
        var index = new CompendiumIndex();
        foreach (var kind in Kinds.All)
            LoadKind(kind, index);
        return index;
    }

    public void LoadKind(KindInfo kind, CompendiumIndex index)
    {
        if (!FileSystem.Current.Exists(kind.File)) return;

        string json = FileSystem.Current.ReadAllText(kind.File);
        if (string.IsNullOrWhiteSpace(json)) return;

        var root = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });

        if (root is not JsonObject obj) return;
        index.SetRoot(kind.Kind, obj);

        if (obj[kind.RootPath] is not JsonArray array) return;

        foreach (var item in array)
        {
            if (item is not JsonObject itemObj) continue;

            string id = itemObj[kind.IdField]?.GetValue<string>() ?? "";
            if (string.IsNullOrEmpty(id)) continue;

            var entry = new CompendiumEntry
            {
                Kind = kind.Kind,
                Id = id,
                Json = itemObj,
                DisplayName = ExtractDisplayName(itemObj, kind),
            };
            entry.SetInitialSnapshot();
            index.Add(entry);
        }
    }

    private static string ExtractDisplayName(JsonObject obj, KindInfo kind)
    {
        var node = obj[kind.DisplayField];
        if (node is JsonObject localized)
        {
            var ru = localized["ru"];
            if (ru is JsonValue rv) return rv.GetValue<string>() ?? obj[kind.IdField]?.GetValue<string>() ?? "?";
            if (ru is JsonObject ro) return ro["nom"]?.GetValue<string>() ?? obj[kind.IdField]?.GetValue<string>() ?? "?";
        }
        return obj[kind.IdField]?.GetValue<string>() ?? "?";
    }
}