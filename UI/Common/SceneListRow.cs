using Godot;

public abstract partial class SceneListRow : Button
{
    public string Id { get; private set; }
    protected bool Hovered { get; private set; }

    public override void _Ready()
    {
        ToggleMode = true;
        ActionMode = ActionModeEnum.Press;

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

    public void SetId(string id) => Id = id;

    public void SetSelected(bool value)
    {
        if (ButtonPressed == value) return;
        ButtonPressed = value;
    }

    protected abstract void RefreshState();

    private void OnMouseEntered()
    {
        Hovered = true;
        RefreshState();
    }

    private void OnMouseExited()
    {
        Hovered = false;
        RefreshState();
    }

    private void OnToggled(bool _)
    {
        RefreshState();
    }
}