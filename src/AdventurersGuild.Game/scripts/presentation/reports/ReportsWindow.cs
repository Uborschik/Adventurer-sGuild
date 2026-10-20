namespace AdventurersGuild.Presentation.Reports;

using System.Linq;
using Godot;
using AdventurersGuild.Application.Reports;
using AdventurersGuild.Presentation.Common;

public partial class ReportsWindow : InteractableWindow
{
    [Export] public PackedScene ReportDayBlockPrefab { get; set; }

    private VBoxContainer list;
    private ReportsRegistry registry;

    public override void _Ready()
    {
        list = GetNode<VBoxContainer>("VBox/Scroll/List");
        ClearList();
    }

    public override void _ExitTree()
    {
        if (registry != null)
            registry.Changed -= OnChanged;
    }

    public void Bind(ReportsRegistry registry)
    {
        this.registry = registry;

        if (this.registry != null)
            this.registry.Changed += OnChanged;

        Rebuild();
    }

    private void OnChanged() => CallDeferred(nameof(Rebuild));

    private void Rebuild()
    {
        ClearList();

        if (registry == null) return;

        // Свежие сверху
        foreach (var day in registry.Days.Reverse())
        {
            var block = ReportDayBlockPrefab.Instantiate<ReportDayBlock>();
            list.AddChild(block);
            block.Bind(day);
        }
    }

    private void ClearList()
    {
        if (list == null) return;
        foreach (var child in list.GetChildren())
            child.QueueFree();
    }
}