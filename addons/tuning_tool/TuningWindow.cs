#if TOOLS
using Godot;

[Tool]
public partial class TuningWindow : Window
{
    public override void _Ready()
    {
        Title = "Tuning Tool";
        Size = new Vector2I(1920, 1080);
        MinSize = new Vector2I(1320, 720);

        var wrapper = new Control();
        AddChild(wrapper);
        wrapper.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var dock = new TuningDock();
        wrapper.AddChild(dock);
        dock.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested()
    {
        Hide();
    }
}
#endif