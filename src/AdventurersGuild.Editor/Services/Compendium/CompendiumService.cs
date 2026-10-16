#nullable enable

namespace AdventurersGuild.Editor.Services.Compendium;

using AdventurersGuild.Editor.Compendium.Loading;

public sealed class CompendiumService
{
    private readonly CompendiumJsonLoader _loader;
    private readonly CompendiumWriter _writer;

    public CompendiumIndex Index { get; private set; } = new();

    public CompendiumService(CompendiumJsonLoader loader, CompendiumWriter writer)
    {
        _loader = loader;
        _writer = writer;
    }

    public void LoadAll() => Index = _loader.LoadAll();
    public void Save(CompendiumEntry entry) => _writer.Save(Index, entry);
    public void SaveNew(CompendiumEntry entry) => _writer.SaveNew(Index, entry);
    public void Delete(CompendiumEntry entry) => _writer.Delete(Index, entry);
}