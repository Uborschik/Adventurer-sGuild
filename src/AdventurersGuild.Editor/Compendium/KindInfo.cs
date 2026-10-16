#nullable enable

namespace AdventurersGuild.Editor.Compendium;

public sealed record KindInfo
{
    public required string Kind { get; init; }
    public required string SingularTitle { get; init; }
    public required string PluralTitle { get; init; }
    public string Section { get; init; } = "Прочее";
    public int Order { get; init; }
    public required string File { get; init; }
    public required string RootPath { get; init; }
    public string IdField { get; init; } = "id";
    public string DisplayField { get; init; } = "name";
}