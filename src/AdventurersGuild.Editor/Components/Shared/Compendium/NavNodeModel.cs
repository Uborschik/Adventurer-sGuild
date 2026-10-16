#nullable enable

namespace AdventurersGuild.Editor.Components.Shared.Compendium;

using AdventurersGuild.Editor.Compendium.Schema;

public sealed class NavNodeModel
{
    public string Name = "";
    public int Order;
    public List<NavNodeModel> Children = [];
    public EntitySchema? Schema;
}