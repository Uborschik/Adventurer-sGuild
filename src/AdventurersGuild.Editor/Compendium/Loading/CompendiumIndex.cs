namespace AdventurersGuild.Editor.Compendium.Loading;

using System.Text.Json.Nodes;

public sealed class CompendiumIndex
{
    private readonly Dictionary<string, Dictionary<string, CompendiumEntry>> _byKind = new();
    private readonly Dictionary<string, JsonObject> _rootsByKind = new();

    public void Clear()
    {
        _byKind.Clear();
        _rootsByKind.Clear();
    }

    public void Add(CompendiumEntry entry)
    {
        if (!_byKind.TryGetValue(entry.Kind, out var bucket))
        {
            bucket = new Dictionary<string, CompendiumEntry>();
            _byKind[entry.Kind] = bucket;
        }

        if (!bucket.TryAdd(entry.Id, entry))
            throw new InvalidOperationException(
                $"Дублирующийся id '{entry.Id}' в kind '{entry.Kind}'.");
    }

    public void SetRoot(string kind, JsonObject root) => _rootsByKind[kind] = root;

    public JsonObject? GetRoot(string kind)
        => _rootsByKind.TryGetValue(kind, out var r) ? r : null;

    public IEnumerable<CompendiumEntry> AllOfKind(string kind)
        => _byKind.TryGetValue(kind, out var bucket)
            ? bucket.Values
            : Enumerable.Empty<CompendiumEntry>();

    public CompendiumEntry? Get(string kind, string id)
        => _byKind.TryGetValue(kind, out var bucket)
           && bucket.TryGetValue(id, out var entry)
            ? entry
            : null;

    public bool Exists(string kind, string id) => Get(kind, id) != null;

    public int CountOf(string kind)
        => _byKind.TryGetValue(kind, out var bucket) ? bucket.Count : 0;
}