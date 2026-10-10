using AdventurersGuild.Data.Adventurer;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Infrastructure;
using AdventurersGuild.Presentation.Common;
using Godot;

namespace AdventurersGuild.Presentation.Adventurers;

public abstract partial class AdventurerRowBase : SceneListRow, IAdventurerRow
{
    protected TextureRect ClassIcon { get; private set; }
    protected TextureRect RaceIcon { get; private set; }
    protected Label NameLabel { get; private set; }
    protected Label LevelLabel { get; private set; }

    protected AdventurerModel Bound { get; private set; }

    public override void _Ready()
    {
        base._Ready();

        ClassIcon = GetNode<TextureRect>("HBox/Class");
        RaceIcon = GetNode<TextureRect>("HBox/Race");
        NameLabel = GetNode<Label>("HBox/Name");
        LevelLabel = GetNode<Label>("HBox/Level");

        OnReady();
    }

    protected virtual void OnReady() { }

    public override void _ExitTree()
    {
        Unsubscribe();
    }

    public virtual void Bind(AdventurerModel model)
    {
        Unsubscribe();

        Bound = model;

        if (Bound == null)
        {
            Clear();
            return;
        }

        SetId(model.Id);

        NameLabel.Text = model.FirstName;
        LevelLabel.Text = model.Level.Number.ToString();

        ClassIcon.Texture = IconLoader.Get("Classes", model.ClassId);
        RaceIcon.Texture = IconLoader.Get("Races", model.RaceId);

        Bound.StatusChanged += RefreshStatus;
        Bound.ProgressChanged += RefreshLevel;

        RefreshStatus();
        RefreshLevel();
    }

    protected virtual void Clear()
    {
        NameLabel.Text = string.Empty;
        LevelLabel.Text = string.Empty;
        ClassIcon.Texture = null;
        RaceIcon.Texture = null;
        TooltipText = string.Empty;
    }

    private void Unsubscribe()
    {
        if (Bound == null) return;

        Bound.StatusChanged -= RefreshStatus;
        Bound.ProgressChanged -= RefreshLevel;
    }

    protected virtual void RefreshStatus() { }

    protected virtual void RefreshLevel()
    {
        if (Bound == null) return;

        LevelLabel.Text = Bound.Level.Number.ToString();
        TooltipText = BuildTooltip();
    }

    protected virtual string BuildTooltip()
        => $"{Bound.FullName} · {AdventurerDatabase.ClassName(Bound.ClassId)} · " +
           $"ур. {Bound.Level.Number} · оп. {Bound.Level.Experience}/{Bound.Level.ExpToNext}";

    protected override void RefreshState()
    {
        var color = ComputeColor();

        ClassIcon.Modulate = color;
        RaceIcon.Modulate = color;
        NameLabel.Modulate = color;
        LevelLabel.Modulate = color;

        ApplyStateColor(color);
    }

    protected virtual void ApplyStateColor(Color color) { }

    protected Color ComputeColor()
    {
        if (Disabled) return new Color(0.5f, 0.5f, 0.5f, 0.5f);
        if (ButtonPressed) return new Color(0.75f, 0.75f, 0.75f);
        if (Hovered) return new Color(1.2f, 1.2f, 1.2f);
        return Colors.White;
    }
}