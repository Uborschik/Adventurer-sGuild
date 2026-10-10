using AdventurersGuild.Domain.Quests;
using AdventurersGuild.Infrastructure;
using AdventurersGuild.Presentation.Common;
using Godot;

namespace AdventurersGuild.Presentation.Quests;

public partial class QuestRow : SceneListRow
{
    private TextureRect icon;
    private Label name;
    private Label difficulty;

    public override void _Ready()
    {
        base._Ready();

        icon = GetNode<TextureRect>("HBox/Icon");
        name = GetNode<Label>("HBox/Name");
        difficulty = GetNode<Label>("HBox/Difficulty");
    }

    public void Bind(QuestModel model)
    {
        SetId(model.Id);

        icon.Texture = IconLoader.Get("Quests", model.TypeId);
        name.Text = model.Name;
    }

    protected override void RefreshState()
    {
        var color = ComputeColor();

        name.Modulate = color;
        difficulty.Modulate = color;
    }

    private Color ComputeColor()
    {
        if (Disabled) return new Color(0.5f, 0.5f, 0.5f, 0.5f);
        if (ButtonPressed) return new Color(0.75f, 0.75f, 0.75f);
        if (Hovered) return new Color(1.2f, 1.2f, 1.2f);
        return Colors.White;
    }
}
