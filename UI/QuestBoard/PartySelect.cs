using Godot;

public partial class PartySelect : Control
{
    private AdventurerRegistry adventurerRegistry;
    private QuestFlow questFlow;
    private QuestResolver questResolver;

    private QuestModel quest;

    private Label chance;
    private Party party;
    private Button apply;

    public Party Party => party;

    private string selectedId;

    public override void _Ready()
    {
        chance = GetNode<Label>("HBox/Chance/Label");
        if (!this.TryGetInstance(out party)) return;
        apply = GetNode<Button>("HBox/Apply/Button");

        party.PartyChanged += RefreshAll;
        party.AddBtn.Pressed += OnAddClicked;
        apply.Pressed += OnApply;

        RefreshAll();
    }

    public override void _ExitTree()
    {
        party.PartyChanged -= RefreshAll;
        party.AddBtn.Pressed -= OnAddClicked;
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

        RefreshAll();
    }

    public void HandleAdventurerClick(string id) => selectedId = id;

    private void OnAddClicked()
    {
        var model = adventurerRegistry.GetById(selectedId);
        if (model == null) return;

        party.Add(model);
    }

    private void OnApply()
    {
        if (quest == null) return;
        if (party.Count == 0) return;

        var list = party.BuildParty();

        if (!questFlow.StartQuest(quest, list)) return;

        RefreshChance();
        party.ClearParty();
        quest = null;
    }

    private void OnQuestCompleted(QuestModel model)
    {
        string gradeName = QuestDatabase.GradeName(model.Result.Role);
        GD.Print($"[Quest] {model.Name}: {gradeName}, " +
                 $"+{model.Result.GoldEarned}g, +{model.Result.GloryEarned}glory");
    }

    private void RefreshAll()
    {
        RefreshChance();
        RefreshCanApply();
    }

    private void RefreshChance()
    {
        if (quest == null) { chance.Text = ""; return; }

        var list = party.BuildParty();
        var prediction = questResolver.Predict(quest, list);

        chance.Text = $"{prediction.PhasesPassed}/{prediction.PhasesTotal} фаз";
    }

    private void RefreshCanApply()
    {
        apply.Disabled = !(quest != null && party.Count > 0);
    }
}