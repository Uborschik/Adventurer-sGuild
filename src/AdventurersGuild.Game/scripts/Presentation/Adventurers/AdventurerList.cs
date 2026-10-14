using System.Collections.Generic;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Presentation.Common;
using Godot;

namespace AdventurersGuild.Presentation.Adventurers;

public partial class AdventurerList : SceneListBase
{
    [Export] public PackedScene RowPrefab { get; set; }

    protected override PackedScene RowScene => RowPrefab;

    public void Add(AdventurerModel model)
    {
        var row = CreateRow();

        if (row is IAdventurerRow adventurerRow)
        {
            adventurerRow.Bind(model);
            return;
        }

        GD.PushError($"{nameof(AdventurerList)}: префаб {row.GetType().Name} " + $"не реализует {nameof(IAdventurerRow)}");

        row.QueueFree();
    }

    public void Remove(string id)
    {
        var row = FindRow(id);
        if (row == null) return;

        RemoveRow(row);

        row.QueueFree();
    }

    public void Refresh(IReadOnlyList<AdventurerModel> items)
    {
        Clear();
        foreach (var m in items)
            Add(m);
    }
}