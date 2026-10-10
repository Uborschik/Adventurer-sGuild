using AdventurersGuild.Infrastructure;
using Godot;

namespace AdventurersGuild.Presentation.Adventurers;

public partial class AdventurerBoardRow : AdventurerRowBase
{
    private TextureRect statusIcon;

    protected override void OnReady()
    {
        statusIcon = GetNode<TextureRect>("HBox/Status");
    }

    protected override void Clear()
    {
        base.Clear();
        statusIcon.Texture = null;
    }

    protected override void RefreshStatus()
    {
        if (Bound == null) return;
        statusIcon.Texture = IconLoader.Get("Statuses", Bound.StatusId);
    }

    protected override void ApplyStateColor(Color color)
    {
        statusIcon.Modulate = color;
    }
}