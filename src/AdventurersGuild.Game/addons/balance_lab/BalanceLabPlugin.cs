namespace AdventurersGuild.Editor.BalanceLab;

#if TOOLS
using Godot;

[Tool]
public partial class BalanceLabPlugin : EditorPlugin
{
    private Window window;

    public override void _EnterTree() => AddToolMenuItem("Open Balance Lab", new Callable(this, MethodName.ShowWindow));

    public override void _ExitTree()
    {
        RemoveToolMenuItem("Open Balance Lab");

        if (IsInstanceValid(window)) window.QueueFree();
    }

    private void ShowWindow()
    {
        if (IsInstanceValid(window))
        {
            window.PopupCentered();
            return;
        }

        window = new Window
        {
            Title = "Balance Lab",
            InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen,
            MinSize = new Vector2I(1100, 700)
        };
        AddChild(window);

        window.PopupCentered();
    }
}
#endif