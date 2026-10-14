using AdventurersGuild.Application.Quests;
using AdventurersGuild.Core;
using AdventurersGuild.Data.Quest;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Quests;
using AdventurersGuild.Presentation.Party;
using Godot;

namespace AdventurersGuild.Presentation.Quests;

public partial class PartySelect : Control
{
    private AdventurerRegistry adventurerRegistry;
    private QuestFlow questFlow;
    private QuestResolver questResolver;

    private QuestModel quest;

    private Slots slots;
    private Button add;
    private Button apply;

    public Slots Slots => slots;

    private string selectedId;

    public override void _Ready()
    {
        if (!this.TryGetInstance(out slots)) return;
        add = GetNode<Button>("VBox/Buttons/Add");
        apply = GetNode<Button>("VBox/Buttons/Apply");

        slots.PartyChanged += RefreshCanApply;
        add.Pressed += OnAdd;
        apply.Pressed += OnApply;

        RefreshCanApply();
    }

    public override void _ExitTree()
    {
        slots.PartyChanged -= RefreshCanApply;
        add.Pressed -= OnAdd;
        apply.Pressed -= OnApply;

        if (questFlow != null)
            questFlow.Completed -= OnQuestCompleted;
    }

    public void Bind(AdventurerRegistry adventurerRegistry, QuestFlow questFlow, QuestResolver questResolver)
    {
        this.adventurerRegistry = adventurerRegistry;
        this.questFlow = questFlow;
        this.questResolver = questResolver;

        if (questFlow != null)
            questFlow.Completed += OnQuestCompleted;
    }

    public void SetQuest(QuestModel newQuest)
    {
        quest = newQuest;

        RefreshCanApply();
    }

    public void HandleAdventurerClick(string id) => selectedId = id;

    private void OnAdd()
    {
        var model = adventurerRegistry.GetById(selectedId);
        if (model == null) return;

        slots.Add(model);
    }

    private void OnApply()
    {
        if (quest == null) return;
        if (slots.Count == 0) return;

        var list = slots.BuildParty();

        if (!questFlow.StartQuest(quest, list)) return;

        slots.ClearParty();
        quest = null;
    }

    private void OnQuestCompleted(QuestModel model)
    {
        string gradeName = QuestDatabase.GradeName(model.Result.Role);
        GD.Print($"[Quest] {model.Name}: {gradeName}, " +
                 $"+{model.Result.GoldEarned}g, +{model.Result.GloryEarned}glory");
    }

    private void RefreshCanApply()
    {
        apply.Disabled = !(quest != null && slots.Count > 0);
    }
}