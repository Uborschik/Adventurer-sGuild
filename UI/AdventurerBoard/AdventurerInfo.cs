using Godot;

public partial class AdventurerInfo : Control
{
    private Label adventurerName;
    private Label adventurerClass;
    private Label might;
    private Label finesse;
    private Label wits;
    private Label presence;

    public override void _Ready()
    {
        adventurerName = GetNode<Label>("VBox/Name");
        adventurerClass = GetNode<Label>("VBox/Class");
        might = GetNode<Label>("VBox/Might");
        finesse = GetNode<Label>("VBox/Finesse");
        wits = GetNode<Label>("VBox/Wits");
        presence = GetNode<Label>("VBox/Presence");

        Clear();
    }

    public void SetAdventurer(AdventurerModel model)
    {
        if (model == null) { Clear(); return; }

        adventurerName.Text = model.FirstName;
        adventurerClass.Text = model.ClassName();

        might.Text = model.StatLine("might");
        finesse.Text = model.StatLine("finesse");
        wits.Text = model.StatLine("wits");
        presence.Text = model.StatLine("presence");
    }

    public void Clear()
    {
        adventurerName.Text = "—";
        adventurerClass.Text = "";
        might.Text = "";
        finesse.Text = "";
        wits.Text = "";
        presence.Text = "";
    }
}