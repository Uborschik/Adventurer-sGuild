using AdventurersGuild.Domain.Quests;
using AdventurersGuild.Presentation.Common;
using Godot;
using System.Collections.Generic;

namespace AdventurersGuild.Presentation.Quests;

public partial class QuestList : SceneListBase
{
    [Export] public PackedScene QuestRowPrefab { get; set; }

    protected override PackedScene RowScene => QuestRowPrefab;

    public void Add(QuestModel model)
    {
        var row = (QuestRow)CreateRow();
        row.Bind(model);
    }

    public void Remove(string id)
    {
        var row = FindRow(id);
        if (row == null) return;

        row.QueueFree();
        RemoveRow(row);
    }

    public void Refresh(IReadOnlyList<QuestModel> list)
    {
        Clear();
        foreach (var i in list)
            Add(i);
    }
}