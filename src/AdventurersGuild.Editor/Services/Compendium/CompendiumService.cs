namespace AdventurersGuild.Editor.Services.Compendium;

using AdventurersGuild.Editor.Compendium.Loading;
using AdventurersGuild.Editor.Compendium.Schema;

public sealed class CompendiumService(CompendiumJsonLoader loader, CompendiumWriter writer, SchemaRegistry schemas)
{
    private readonly CompendiumJsonLoader _loader = loader;
    private readonly CompendiumWriter _writer = writer;
    private readonly SchemaRegistry _schemas = schemas;

    public CompendiumIndex Index { get; private set; } = new();

    public void LoadAll()
    {
        Index = _loader.LoadAll();
    }

    public void Save(CompendiumEntry entry)
    {
        _writer.Save(Index, entry);
    }

    public void SaveNew(CompendiumEntry entry)
    {
        _writer.SaveNew(Index, entry);
    }

    public void Delete(CompendiumEntry entry)
    {
        _writer.Delete(Index, entry);
    }

    public EntitySchema Schema(string kind) => _schemas.Get(kind);
}