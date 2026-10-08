#if TOOLS
using Godot;

[Tool]
public partial class TuningPlugin : EditorPlugin
{
    private const string ToolMenuName = "Tuning Tool";

    private TuningWindow _window;
    private Button _toolbarButton;

    public override void _EnterTree()
    {
        AddToolMenuItem(ToolMenuName, Callable.From(OpenWindow));

        _toolbarButton = new Button
        {
            Text = "Tuning",
            TooltipText = "Open Tuning Tool (Ctrl+Shift+T)",
            Flat = true,
        };
        _toolbarButton.Pressed += OpenWindow;

        var shortcut = new Shortcut();
        shortcut.Events = new Godot.Collections.Array();
        shortcut.Events.Add(new InputEventKey
        {
            Keycode = Key.T,
            CtrlPressed = true,
            ShiftPressed = true,
        });
        _toolbarButton.Shortcut = shortcut;

        AddControlToContainer(CustomControlContainer.Toolbar, _toolbarButton);
    }

    public override void _ExitTree()
    {
        RemoveToolMenuItem(ToolMenuName);

        if (_toolbarButton != null)
        {
            RemoveControlFromContainer(CustomControlContainer.Toolbar, _toolbarButton);
            _toolbarButton.QueueFree();
            _toolbarButton = null;
        }

        if (_window != null)
        {
            _window.QueueFree();
            _window = null;
        }
    }

    private void OpenWindow()
    {
        if (_window == null || !IsInstanceValid(_window))
        {
            _window = new TuningWindow();

            EditorInterface.Singleton.GetBaseControl().AddChild(_window);
        }

        _window.PopupCentered(new Vector2I(900, 650));
    }
}
#endif