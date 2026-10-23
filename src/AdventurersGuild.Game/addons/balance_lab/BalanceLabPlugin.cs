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
            Size = new Vector2I(1270, 720),
            MinSize = new Vector2I(1270, 720),
        };

        AutoTranslateMode = AutoTranslateModeEnum.Disabled;

        window.CloseRequested += () =>
        {
            window.QueueFree();
            window = null;
        };

        AddChild(window);

        var mainContent = new BalanceLabMain();
        mainContent.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        window.AddChild(mainContent);

        window.PopupCentered();
    }
}
#endif