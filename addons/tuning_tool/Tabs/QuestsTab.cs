#if TOOLS
using Godot;

[Tool]
public partial class QuestsTab : Control
{
    public override void _Ready()
    {
        var tabs = new TabContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        AddChild(tabs);
        tabs.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        tabs.TabAlignment = TabBar.AlignmentMode.Center;

        AddSub(tabs, "Templates", new TemplatesTab());
        AddSub(tabs, "Locations", new LocationsTab());
        AddSub(tabs, "Creatures", new CreaturesTab());
        AddSub(tabs, "Phases", new PhasesTab());
        AddSub(tabs, "Balance", new BalanceTab());
    }

    private static void AddSub(TabContainer tabs, string name, Control content)
    {
        content.Name = name;
        tabs.AddChild(content);
    }
}
#endif