namespace AdventurersGuild.Editor.Compendium.Loading;

using System.Text.Json.Nodes;

public sealed class CompendiumEntry
{
    public required string Kind { get; init; }
    public required string Id { get; init; }

    /// <summary>JSON-объект — это узел в дереве загруженного файла. Мутации видны в файле.</summary>
    public required JsonObject Json { get; init; }

    /// <summary>Отображаемое имя для списка.</summary>
    public required string DisplayName { get; set; }

    /// <summary>Снапшот на момент последнего сохранения. Для dirty-check.</summary>
    public string Snapshot { get; private set; } = "";

    public bool IsDirty => Snapshot != Json.ToJsonString();

    public void RefreshSnapshot() => Snapshot = Json.ToJsonString();

    public void SetInitialSnapshot() => RefreshSnapshot();
}