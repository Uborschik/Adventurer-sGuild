using System;
using System.Collections.Generic;
using Godot;

namespace AdventurersGuild.Presentation.Common;

public abstract partial class SceneListBase : Control
{
    public event Action<string> SelectedIdChanged;

    protected VBoxContainer Container { get; private set; }

    private readonly List<SceneListRow> rows = [];
    private readonly SingleSelection<SceneListRow> selection = new();

    protected abstract PackedScene RowScene { get; }

    public override void _Ready()
    {
        Container = GetNode<VBoxContainer>("Scroll/VBox");
        OnReadyInternal();
    }

    protected virtual void OnReadyInternal() { }

    protected SceneListRow CreateRow()
    {
        var row = RowScene.Instantiate<SceneListRow>();
        Container.AddChild(row);

        row.Toggled += pressed => OnRowToggled(row, pressed);

        rows.Add(row);
        return row;
    }

    protected SceneListRow FindRow(string id)
    {
        foreach (var row in rows)
            if (row.Id == id) return row;
        return null;
    }

    protected void RemoveRow(SceneListRow row)
    {
        selection.Forget(row);
        rows.Remove(row);
    }

    public void Clear()
    {
        foreach (var row in rows)
            row.QueueFree();

        rows.Clear();
        selection.Deselect();
    }

    public void Deselect()
    {
        selection.Deselect();
    }

    private void OnRowToggled(SceneListRow row, bool pressed)
    {
        if (!selection.OnToggled(row, pressed)) return;

        if (pressed)
            SelectedIdChanged?.Invoke(row.Id);
        else
            SelectedIdChanged?.Invoke(null);
    }
}