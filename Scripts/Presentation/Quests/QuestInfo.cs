using AdventurersGuild.Core;
using AdventurersGuild.Data.Quest;
using AdventurersGuild.Domain.Quests;
using Godot;
using System.Linq;

namespace AdventurersGuild.Presentation.Quests;

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

        name.Text = QuestDatabase.TypeName(model.TypeId);


        description.Text = model.Description;

        // Фазы
        var lang = Loc.Language;
        var phaseNames = model.Phases == null
            ? "—"
            : string.Join(", ", model.Phases.Select(p => p.Name.Get(lang) ?? p.Id));
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
        duration.Text = "";
        expires.Text = "";
        reward.Text = "";
    }
}