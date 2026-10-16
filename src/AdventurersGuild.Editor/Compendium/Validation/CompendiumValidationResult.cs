#nullable enable

namespace AdventurersGuild.Editor.Compendium.Validation;

public sealed class CompendiumValidationResult
{
    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];

    public bool HasErrors => Errors.Count > 0;

    public void Error(string message) => Errors.Add(message);
    public void Warn(string message) => Warnings.Add(message);
}