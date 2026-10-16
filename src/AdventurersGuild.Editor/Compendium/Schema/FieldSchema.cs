namespace AdventurersGuild.Editor.Compendium.Schema;

public sealed class FieldSchema
{
    public required string Name { get; init; }
    public string? Label { get; init; }
    public string? Hint { get; init; }

    public required CompendiumFieldType Type { get; init; }
    public bool Required { get; init; }

    /// <summary>Kind цели — для Reference / ReferenceList / Dictionary.</summary>
    public string? TargetKind { get; init; }

    /// <summary>Допустимые значения для Enum.</summary>
    public string[]? Values { get; init; }

    /// <summary>Границы для Number.</summary>
    public double? Min { get; init; }
    public double? Max { get; init; }
    public double? Step { get; init; }

    /// <summary>Имя подсхемы для Nested / ObjectList.</summary>
    public string? NestedSchemaName { get; init; }

    public string? OptionsFromParentArray { get; init; }

    /// <summary>Группа полей (визуальный блок).</summary>
    public string? Group { get; init; }

    /// <summary>Ширина в MudGrid (12-колоночная).</summary>
    public int Width { get; init; } = 12;

    /// <summary>Порядок внутри группы.</summary>
    public int Order { get; init; }
}