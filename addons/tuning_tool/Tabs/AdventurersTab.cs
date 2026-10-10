#if TOOLS
using Godot;

[Tool]
public partial class AdventurersTab : Control
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

        AddSub(tabs, "Classes", new ClassesTab());
        AddSub(tabs, "Races", new RacesTab());
        AddSub(tabs, "Progression", new ProgressionTab());
    }

    private static void AddSub(TabContainer tabs, string name, Control content)
    {
        content.Name = name;
        tabs.AddChild(content);
    }
}
#endif