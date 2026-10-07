using System;

public partial class QuestBoardWindow : InteractableWindow
{
    private AdventurerRegistry adventurerRegistry;
    private QuestRegistry questRegistry;
    private QuestFlow questFlow;

    private QuestList questList;
    private QuestInfo questInfo;
    private AdventurerBoard adventurerBoard;
    private AdventurerInfo adventurerInfo;
    private PartySelect partySelect;

    public override void _Ready()
    {
        if (!this.TryGetInstance(out questList)) return;
        if (!this.TryGetInstance(out questInfo)) return;
        if (!this.TryGetInstance(out adventurerBoard)) return;
        if (!this.TryGetInstance(out adventurerInfo)) return;
        if (!this.TryGetInstance(out partySelect)) return;

        questList.SelectedIdChanged += OnQuestSelected;
        adventurerBoard.AdventurerList.SelectedIdChanged += OnAdventurerSelected;

        var partyControl = partySelect?.Party;

        if (partyControl != null)
            partyControl.SelectedAdventurer += OnSelectAdventure;

        questInfo.Clear();
    }

    public override void _ExitTree()
    {
        if (questList != null) questList.SelectedIdChanged -= OnQuestSelected;
        if (adventurerBoard?.AdventurerList != null)
            adventurerBoard.AdventurerList.SelectedIdChanged -= OnAdventurerSelected;

        var partyControl = partySelect?.Party;

        if (partyControl != null)
            partyControl.SelectedAdventurer -= OnSelectAdventure;

        if (questFlow != null)
        {
            questFlow.Added -= OnQuestAdded;
            questFlow.Expired -= OnQuestExpired;
        }
    }

    public void Bind(AdventurerRegistry adventurerRegistry, QuestRegistry questRegistry, QuestFlow questFlow, QuestResolver questResolver)
    {
        this.adventurerRegistry = adventurerRegistry;
        this.questRegistry = questRegistry;
        this.questFlow = questFlow;

        adventurerBoard.Bind(adventurerRegistry);
        partySelect.Bind(adventurerRegistry, questFlow, questResolver);
    }

    public void Start()
    {
        if (questFlow == null) return;

        questFlow.Added += OnQuestAdded;
        questFlow.Expired += OnQuestExpired;
        questFlow.Started += OnQuestStarted;

        adventurerBoard.Refresh();
        RefreshQuestList();
    }

    private void OnQuestAdded(QuestModel quest) => questList.Add(quest);

    private void OnQuestExpired(QuestModel quest) => questList.Remove(quest.Id);

    private void OnQuestStarted(QuestModel quest)
    {
        questList.Remove(quest.Id);

        if (questInfo != null)
            questInfo.Clear();
    }

    private void OnQuestSelected(string id)
    {
        var quest = questRegistry.GetById(id);
        if (quest == null) return;

        questInfo.Display(quest);
        partySelect.SetQuest(quest);
    }

    private void OnAdventurerSelected(string id)
    {
        var model = adventurerRegistry.GetById(id);

        adventurerInfo.SetAdventurer(model);
        partySelect.HandleAdventurerClick(id);
    }

    private void OnSelectAdventure(AdventurerModel model)
    {
        adventurerInfo.SetAdventurer(model);
    }

    private void RefreshQuestList()
    {
        questList.Clear();
        foreach (var q in questRegistry.Available)
            questList.Add(q);
    }
}