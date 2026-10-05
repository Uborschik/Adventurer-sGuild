using Godot;

public partial class Context : Control
{
    private Button hireBtn;
    private Button rejectBtn;

    private Control btnContainer;

    public override void _Ready()
    {
        btnContainer = GetNode<Control>("HBox");
        hireBtn = btnContainer.GetNode<Button>("Hire");
        rejectBtn = btnContainer.GetNode<Button>("Reject");
    }

    public void ShowContainer() => btnContainer.Visible = true;
    public void HideContainer() => btnContainer.Visible = false;

    public void SetActiveRegisterButtons(bool active)
    {
        if (hireBtn.Disabled == !active) return;

        hireBtn.Disabled = !active;
        rejectBtn.Disabled = !active;
    }
}
