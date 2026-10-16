#nullable enable

namespace AdventurersGuild.Editor.Compendium.Validation;

using System.Text.Json.Nodes;
using AdventurersGuild.Editor.Compendium.Loading;
using AdventurersGuild.Editor.Compendium.Schema;
using AdventurersGuild.Editor.Services.Compendium;

public sealed class EntityValidator(SchemaRegistry schemas, CompendiumService service)
{
    private readonly SchemaRegistry _schemas = schemas;
    private readonly CompendiumIndex _index = service.Index;

    public CompendiumValidationResult Validate(EntitySchema schema, JsonObject entity)
    {
        var result = new CompendiumValidationResult();

        ValidateObject(schema, entity, "", result);
        ValidateIdUniquenessInLists(schema, entity, "", result);

        return result;
    }

    // === Рекурсивная проверка полей ===

    private void ValidateObject(EntitySchema schema, JsonObject obj, string path, CompendiumValidationResult result)
    {
        foreach (var field in schema.Fields)
        {
            string fieldPath = string.IsNullOrEmpty(path)
                ? field.Label ?? field.Name
                : $"{path} → {field.Label ?? field.Name}";

            var node = obj[field.Name];

            // Localized — особый случай: проверяем ru и en отдельно
            if (field.Type == CompendiumFieldType.Localized)
            {
                if (field.Required && !HasValidLocalized(node))
                    result.Error($"Поле «{fieldPath}» обязательно (нужны RU и EN)");
                continue;
            }

            if (field.Type == CompendiumFieldType.LocalizedCase)
            {
                if (field.Required && !HasValidLocalizedCase(node))
                    result.Error($"Поле «{fieldPath}» обязательно (нужны RU именительный и EN)");
                continue;
            }

            if (field.Required && IsEmpty(node))
            {
                result.Error($"Поле «{fieldPath}» обязательно");
                continue;
            }

            if (node == null) continue;

            switch (field.Type)
            {
                case CompendiumFieldType.Reference:
                    ValidateReference(field, node, fieldPath, result);
                    break;
                case CompendiumFieldType.ReferenceList:
                    ValidateReferenceList(field, node, fieldPath, result);
                    break;
                case CompendiumFieldType.Dictionary:
                    ValidateDictionary(field, node, fieldPath, result);
                    break;
                case CompendiumFieldType.Nested:
                    if (node is JsonObject nestedObj
                        && !string.IsNullOrEmpty(field.NestedSchemaName)
                        && _schemas.TryGetInternal(field.NestedSchemaName, out var nestedSchema)
                        && nestedSchema != null)
                    {
                        ValidateObject(nestedSchema, nestedObj, fieldPath, result);
                    }
                    break;
                case CompendiumFieldType.ObjectList:
                    if (node is JsonArray arr
                        && !string.IsNullOrEmpty(field.NestedSchemaName)
                        && _schemas.TryGetInternal(field.NestedSchemaName, out var listSchema)
                        && listSchema != null)
                    {
                        for (int i = 0; i < arr.Count; i++)
                        {
                            if (arr[i] is JsonObject itemObj)
                                ValidateObject(listSchema, itemObj, $"{fieldPath}[{i + 1}]", result);
                        }
                    }
                    break;
            }
        }
    }

    private static bool HasValidLocalized(JsonNode? node)
    {
        if (node is not JsonObject obj) return false;

        string? ru = obj["ru"]?.GetValue<string>();
        string? en = obj["en"]?.GetValue<string>();

        return !string.IsNullOrWhiteSpace(ru) && !string.IsNullOrWhiteSpace(en);
    }

    private static bool HasValidLocalizedCase(JsonNode? node)
    {
        if (node is not JsonObject obj) return false;

        string? ruNom = (obj["ru"] as JsonObject)?["nom"]?.GetValue<string>();
        string? en = obj["en"]?.GetValue<string>();

        return !string.IsNullOrWhiteSpace(ruNom) && !string.IsNullOrWhiteSpace(en);
    }

    private void ValidateReference(FieldSchema field, JsonNode node, string fieldPath, CompendiumValidationResult result)
    {
        if (string.IsNullOrEmpty(field.TargetKind)) return;

        string? id = node.GetValue<string>();
        if (string.IsNullOrEmpty(id)) return;

        if (!_index.Exists(field.TargetKind, id))
        {
            result.Error($"Поле «{fieldPath}»: «{id}» не найден в {field.TargetKind}");
        }
    }

    private void ValidateReferenceList(FieldSchema field, JsonNode node, string fieldPath, CompendiumValidationResult result)
    {
        if (string.IsNullOrEmpty(field.TargetKind)) return;
        if (node is not JsonArray arr) return;

        foreach (var item in arr)
        {
            string? id = item?.GetValue<string>();
            if (string.IsNullOrEmpty(id)) continue;

            if (!_index.Exists(field.TargetKind, id))
            {
                result.Error($"Поле «{fieldPath}»: «{id}» не найден в {field.TargetKind}");
            }
        }
    }

    private void ValidateDictionary(FieldSchema field, JsonNode node, string fieldPath, CompendiumValidationResult result)
    {
        if (string.IsNullOrEmpty(field.TargetKind)) return;
        if (node is not JsonObject obj) return;

        foreach (var kv in obj)
        {
            if (!_index.Exists(field.TargetKind, kv.Key))
            {
                result.Error($"Поле «{fieldPath}»: ключ «{kv.Key}» не найден в {field.TargetKind}");
            }
        }
    }

    // === Проверка дубликатов ID в списках объектов ===

    private void ValidateIdUniquenessInLists(EntitySchema schema, JsonObject obj, string path, CompendiumValidationResult result)
    {
        foreach (var field in schema.Fields)
        {
            if (field.Type != CompendiumFieldType.ObjectList) continue;
            if (string.IsNullOrEmpty(field.NestedSchemaName)) continue;
            if (!_schemas.TryGetInternal(field.NestedSchemaName, out var nestedSchema) || nestedSchema == null) continue;

            string fieldPath = string.IsNullOrEmpty(path) ? field.Label ?? field.Name : $"{path} → {field.Label ?? field.Name}";

            if (obj[field.Name] is not JsonArray arr) continue;

            var seen = new HashSet<string>();
            var idField = nestedSchema.IdField;

            for (int i = 0; i < arr.Count; i++)
            {
                if (arr[i] is not JsonObject item) continue;

                string? id = item[idField]?.GetValue<string>();
                if (string.IsNullOrEmpty(id)) continue;

                if (!seen.Add(id))
                {
                    result.Error($"Поле «{fieldPath}»: дублирующийся ID «{id}»");
                }

                // Рекурсия — вложенные списки внутри
                ValidateIdUniquenessInLists(nestedSchema, item, $"{fieldPath}[{i + 1}]", result);
            }
        }
    }

    private static bool IsEmpty(JsonNode? node)
    {
        if (node == null) return true;
        if (node is JsonValue v)
        {
            if (v.TryGetValue<string>(out var s)) return string.IsNullOrWhiteSpace(s);
        }
        if (node is JsonArray arr) return arr.Count == 0;
        if (node is JsonObject obj) return obj.Count == 0;
        return false;
    }
}