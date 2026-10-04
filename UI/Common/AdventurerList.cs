using System.Collections.Generic;
using Godot;

public partial class AdventurerList : SceneListBase
{
    [Export] public PackedScene AdventurerRowPrefab { get; set; }

    protected override PackedScene RowScene => AdventurerRowPrefab;

    public void Add(AdventurerModel model)
    {
        var row = (AdventurerRow)CreateRow();
        row.Bind(model);
    }

    public void Remove(string id)
    {
        var row = FindRow(id);
        if (row == null) return;

        row.QueueFree();
        RemoveRow(row);
    }

    public void Refresh(IReadOnlyList<AdventurerModel> hired)
    {
        Clear();
        foreach (var h in hired)
            Add(h);
    }
}