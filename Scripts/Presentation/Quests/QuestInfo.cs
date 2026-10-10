namespace AdventurersGuild.Presentation.Quests;

using System.Linq;
using Godot;
using AdventurersGuild.Core;
using AdventurersGuild.Data.Quest;
using AdventurersGuild.Domain.Quests;

public partial class QuestInfo : Control
{
    private Label name;
    private RichTextLabel description;
    private Label phases;
    private Label duration;
    private Label expires;
    private Label reward;

    public override void _Ready()
    {
        name = GetNode<Label>("VBox/Header/Name");
        description = GetNode<RichTextLabel>("VBox/Description");
        phases = GetNode<Label>("VBox/Stats/PartySize");
        duration = GetNode<Label>("VBox/Stats/Duration");
        expires = GetNode<Label>("VBox/Stats/Expires");
        reward = GetNode<Label>("VBox/Reward");

        Clear();
    }

    public void Display(QuestModel model)
    {
        if (model == null) { Clear(); return; }

        name.Text = QuestDatabase.BlueprintName(model.BlueprintId);

        var target = QuestDatabase.GetCreature(model.TargetId);
        var location = QuestDatabase.GetLocation(model.LocationId);

        string lang = Loc.Language;
        string targetName = target?.Name?.Get(lang) ?? model.TargetId ?? "—";
        string locationName = location?.Name?.Get(lang) ?? model.LocationId ?? "—";

        string descrText = model.Description ?? "";
        description.Text = $"{descrText}\n\n{Loc.Get("quest.target")}: {targetName}\n" +
                           $"{Loc.Get("quest.location")}: {locationName}";

        var phaseNames = model.Phases == null || model.Phases.Count == 0
            ? "—"
            : string.Join(", ", model.Phases
                .Select(p => p.Name.Get(lang) ?? p.PhaseId));

        phases.Text = $"Испытания: {phaseNames}";

        expires.Text = $"{Loc.Get("quest.expires")}: {Loc.FormatTime(model.ExpiresAt)}";

        var gold = $"{Loc.Get("quest.gold")}: {model.GoldReward}";
        var glory = $"{Loc.Get("quest.glory")}: {model.GloryReward}";
        reward.Text = $"{gold}  {glory}";
    }

    public void Clear()
    {
        name.Text = "—";
        description.Text = Loc.Get("quest.select_hint");
        phases.Text = "";
        if (duration != null) duration.Text = "";
        expires.Text = "";
        reward.Text = "";
    }
}