using Godot;

public partial class NavigationButton : Button
{
    [Export] public WindowType Window { get; set; } = WindowType.None;

    private InteractableWindow targetWindow;
    private TextureRect buttonIcon;
    private bool hovered;

    public override void _Ready()
    {
        ToggleMode = true;
        ActionMode = ActionModeEnum.Press;

        buttonIcon = GetNodeOrNull<TextureRect>("Icon");
        if (buttonIcon != null)
            buttonIcon.MouseFilter = MouseFilterEnum.Ignore;

        MouseFilter = MouseFilterEnum.Stop;

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        Toggled += OnToggled;
    }

    public override void _ExitTree()
    {
        MouseEntered -= OnMouseEntered;
        MouseExited -= OnMouseExited;
        Toggled -= OnToggled;
    }

    public void Bind(InteractableWindow window)
    {
        targetWindow = window;
        window?.SetState(false);
    }

    public void SetSelected(bool value)
    {
        if (ButtonPressed == value) return;
        ButtonPressed = value;
    }

    private void OnToggled(bool pressed)
    {
        RefreshIcon();
        targetWindow?.SetState(pressed);
    }

    private void RefreshIcon()
    {
        if (buttonIcon == null) return;
        buttonIcon.Modulate = ComputeColor();
    }

    private Color ComputeColor()
    {
        if (Disabled) return new Color(0.5f, 0.5f, 0.5f, 0.5f);
        if (ButtonPressed) return new Color(0.75f, 0.75f, 0.75f);
        if (hovered) return new Color(1.2f, 1.2f, 1.2f);
        return Colors.White;
    }

    private void OnMouseEntered()
    {
        hovered = true;
        RefreshIcon();
    }

    private void OnMouseExited()
    {
        hovered = false;
        RefreshIcon();
    }
}