namespace AdventurersGuild.Editor.Compendium.Schema;

public sealed class EntitySchema
{
    public required string Kind { get; init; }
    public required string SingularTitle { get; init; }
    public required string PluralTitle { get; init; }

    /// <summary>Путь к JSON-файлу относительно Data/. Например, "Data/Quest/Tags.json".</summary>
    public string? File { get; init; }

    /// <summary>Путь к массиву внутри JSON. Например, "tags".</summary>
    public string? RootPath { get; init; }

    public required IReadOnlyList<FieldSchema> Fields { get; init; }
    public string? Section { get; init; }
    public int Order { get; init; }

    /// <summary>Поле для отображения в списке. По умолчанию "name".</summary>
    public string DisplayField { get; init; } = "name";

    /// <summary>Имя поля-идентификатора. По умолчанию "id".</summary>
    public string IdField { get; init; } = "id";

    public IEnumerable<IGrouping<string, FieldSchema>> GroupedFields =>
        Fields
            .GroupBy(f => f.Group ?? "Основное")
            .OrderBy(g => g.Min(f => f.Order));
}