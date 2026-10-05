using Godot;

public partial class AdventurerRow : SceneListRow
{
    private TextureRect classIcon;
    private TextureRect raceIcon;
    private Label nameLabel;
    private Label levelLabel;
    private TextureRect statusIcon;

    private AdventurerModel bound;

    public override void _Ready()
    {
        base._Ready();

        classIcon = GetNode<TextureRect>("HBox/Class");
        raceIcon = GetNode<TextureRect>("HBox/Race");
        nameLabel = GetNode<Label>("HBox/Name");
        levelLabel = GetNode<Label>("HBox/Level");
        statusIcon = GetNode<TextureRect>("HBox/Status");
    }

    public override void _ExitTree()
    {
        if (bound != null)
            bound.StatusChanged -= RefreshStatus;
    }

    public void Bind(AdventurerModel model)
    {
        if (bound != null)
            bound.StatusChanged -= RefreshStatus;

        bound = model;

        if (bound == null) return;
        SetId(model.Id);

        nameLabel.Text = model.FirstName;
        levelLabel.Text = model.Level.Number.ToString();

        classIcon.Texture = IconLoader.Get("Classes", model.ClassId);
        raceIcon.Texture = IconLoader.Get("Races", model.RaceId);

        statusIcon.Texture = IconLoader.Get("Statuses", model.StatusId);

        TooltipText = $"{model.FullName} · {AdventurerDatabase.ClassName(model.ClassId)} · ур. {model.Level} · оп. {model.Level.Experience}/{model.Level.ExpToNext}";

        bound.StatusChanged += RefreshStatus;
        bound.ProgressChanged += RefreshLevel;
    }

    protected override void RefreshState()
    {
        var color = ComputeColor();

        classIcon.Modulate = color;
        raceIcon.Modulate = color;
        nameLabel.Modulate = color;
        levelLabel.Modulate = color;
        statusIcon.Modulate = color;
    }

    private void RefreshStatus()
    {
        if (bound == null) return;
        statusIcon.Texture = IconLoader.Get("Statuses", bound.StatusId);
    }

    private void RefreshLevel()
    {
        var name = $"{bound.FullName}";
        var cls = $"{AdventurerDatabase.ClassName(bound.ClassId)}";
        var lvl = $"{bound.Level.Number}";
        var exp = $"{bound.Level.Experience}/{bound.Level.ExpToNext}";

        TooltipText = $"{name} · {cls} · ур. {lvl} · оп. {exp}";
    }

    private Color ComputeColor()
    {
        if (Disabled) return new Color(0.5f, 0.5f, 0.5f, 0.5f);
        if (ButtonPressed) return new Color(0.75f, 0.75f, 0.75f);
        if (Hovered) return new Color(1.2f, 1.2f, 1.2f);
        return Colors.White;
    }
}