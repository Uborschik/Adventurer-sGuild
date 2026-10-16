namespace AdventurersGuild.Editor.Compendium.Schema;

public sealed class SchemaRegistry
{
    private readonly Dictionary<string, EntitySchema> _byKind = new();
    private readonly Dictionary<string, EntitySchema> _internal = new();
    private readonly List<EntitySchema> _ordered = new();

    public IReadOnlyList<EntitySchema> All => _ordered;

    public EntitySchema Get(string kind)
    {
        if (!_byKind.TryGetValue(kind, out var schema))
            throw new InvalidOperationException($"Kind '{kind}' не зарегистрирован.");
        return schema;
    }

    public bool TryGet(string kind, out EntitySchema? schema)
        => _byKind.TryGetValue(kind, out schema);

    public void Register(EntitySchema schema)
    {
        if (!_byKind.TryAdd(schema.Kind, schema))
            throw new InvalidOperationException($"Kind '{schema.Kind}' уже зарегистрирован.");
        _ordered.Add(schema);
        _ordered.Sort((a, b) => a.Order.CompareTo(b.Order));
    }

    public void RegisterInternal(string name, EntitySchema schema)
    {
        _internal[name] = schema;
    }

    public EntitySchema GetInternal(string name)
    {
        if (!_internal.TryGetValue(name, out var schema))
            throw new InvalidOperationException($"Внутренняя схема '{name}' не зарегистрирована.");
        return schema;
    }

    public bool TryGetInternal(string name, out EntitySchema? schema)
        => _internal.TryGetValue(name, out schema);
}