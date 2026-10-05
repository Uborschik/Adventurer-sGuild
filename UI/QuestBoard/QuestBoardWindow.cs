public partial class QuestBoardWindow : InteractableWindow
{
    private AdventurerBoard adventurerBoard;
    private QuestRegistry questRegistry;
    private QuestFlow questFlow;

    private QuestList questList;
    private QuestInfo questInfo;
    private PartySelect partySelect;

    public override void _Ready()
    {
        if (!this.TryGetInstance(out questList)) return;
        if (!this.TryGetInstance(out questInfo)) return;
        if (!this.TryGetInstance(out partySelect)) return;

        questList.SelectedIdChanged += OnQuestClicked;

        questInfo.Clear();
    }

    public override void _ExitTree()
    {
        if (questList != null) questList.SelectedIdChanged -= OnQuestClicked;

        if (questFlow != null)
        {
            questFlow.Added -= OnQuestAdded;
            questFlow.Expired -= OnQuestExpired;
        }
    }

    public void Start()
    {

        if (questFlow == null) return;

        questFlow.Added += OnQuestAdded;
        questFlow.Expired += OnQuestExpired;
        questFlow.Started += OnQuestStarted;

        RefreshList();
    }

    protected override void OnOpen()
    {
        var list = adventurerBoard?.AdventurerList;

        if (list != null)
        {
            list.Deselect();
            list.SelectedIdChanged += partySelect.HandleAdventurerClick;
        }

        var partyControl = partySelect?.Party;

        if (partyControl != null)
            partyControl.SelectedAdventurer += OnSelectAdventure;
    }

    protected override void OnClose()
    {
        var list = adventurerBoard?.AdventurerList;

        if (list != null)
        {
            list.Deselect();
            list.SelectedIdChanged -= partySelect.HandleAdventurerClick;
        }

        var partyControl = partySelect?.Party;

        if (partyControl != null)
            partyControl.SelectedAdventurer -= OnSelectAdventure;
    }

    public void Bind(AdventurerBoard adventurerBoard, QuestRegistry questRegistry, QuestFlow questFlow)
    {
        this.adventurerBoard = adventurerBoard;
        this.questRegistry = questRegistry;
        this.questFlow = questFlow;
    }

    private void OnQuestAdded(QuestModel quest) => questList.Add(quest);

    private void OnQuestExpired(QuestModel quest) => questList.Remove(quest.Id);

    private void OnQuestStarted(QuestModel quest)
    {
        questList.Remove(quest.Id);

        if (questInfo != null)
            questInfo.Clear();
    }

    private void OnQuestClicked(string id)
    {
        var quest = questRegistry.GetById(id);
        if (quest == null) return;

        questInfo.Display(quest);
        partySelect.SetQuest(quest);
    }

    private void OnSelectAdventure(AdventurerModel model)
    {
        adventurerBoard.AdventurerInfo.SetAdventurer(model);
    }

    private void RefreshList()
    {
        questList.Clear();
        foreach (var q in questRegistry.Available)
            questList.Add(q);
    }
}