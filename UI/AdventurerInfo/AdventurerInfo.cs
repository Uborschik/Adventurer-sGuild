using System.Collections.Generic;
using Godot;

public partial class AdventurerInfo : Control
{
    private Control container;
    private Label adventurerName;
    private Label adventurerClass;
    private List<Label> statLabels = [];

    public override void _Ready()
    {
        container = GetNode<Control>("VBox");
        adventurerName = container.GetNode<Label>("Name");
        adventurerClass = container.GetNode<Label>("Class");

        foreach (var stat in StatIds.All)
        {
            var lbl = new Label();
            lbl.Name = stat;
            statLabels.Add(lbl);

            container.AddChild(lbl);
        }

        Clear();
    }

    public void SetAdventurer(AdventurerModel model)
    {
        if (model == null) { Clear(); return; }

        adventurerName.Text = model.FirstName;
        adventurerClass.Text = model.ClassName();

        for (int i = 0; i < statLabels.Count; i++)
        {
            var lbl = statLabels[i];
            var stat = StatIds.All[i];

            lbl.Text = $"{AdventurerDatabase.StatName(stat)}: {(int)model.Stats[stat]}";
        }
    }

    public void Clear()
    {
        adventurerName.Text = "—";
        adventurerClass.Text = "";

        for (int i = 0; i < statLabels.Count; i++)
        {
            var lbl = statLabels[i];

            lbl.Text = "";
        }
    }
}