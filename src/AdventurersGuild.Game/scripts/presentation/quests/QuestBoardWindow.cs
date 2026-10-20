namespace AdventurersGuild.Presentation.Quests;

using AdventurersGuild.Application.Quests;
using AdventurersGuild.Core;
using AdventurersGuild.Domain.Units;
using AdventurersGuild.Domain.Quests;
using AdventurersGuild.Presentation.Adventurers;
using AdventurersGuild.Presentation.Common;

public partial class QuestBoardWindow : InteractableWindow
{
    private UnitRegistry adventurerRegistry;
    private QuestRegistry questRegistry;

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

        var partyControl = partySelect?.Slots;

        if (partyControl != null)
            partyControl.SelectedAdventurer += OnSelectAdventure;

        questInfo.Clear();
    }

    public override void _ExitTree()
    {
        if (questList != null) questList.SelectedIdChanged -= OnQuestSelected;
        if (adventurerBoard?.AdventurerList != null)
            adventurerBoard.AdventurerList.SelectedIdChanged -= OnAdventurerSelected;

        var partyControl = partySelect?.Slots;

        if (partyControl != null)
            partyControl.SelectedAdventurer -= OnSelectAdventure;

        if (questRegistry != null)
        {
            questRegistry.Added -= OnQuestAdded;
        }
    }

    public void Bind(UnitRegistry adventurerRegistry, QuestRegistry questRegistry, QuestFlow questFlow, QuestResolver questResolver)
    {
        this.adventurerRegistry = adventurerRegistry;
        this.questRegistry = questRegistry;

        questRegistry.Added += OnQuestAdded;
        adventurerBoard.Bind(adventurerRegistry);
        partySelect.Bind(adventurerRegistry, questFlow, questResolver);
    }

    private void OnQuestAdded(QuestModel quest)
    {
        switch (quest.Status)
        {
            case QuestStatus.Available:
                questList.Add(quest);
                break;

            case QuestStatus.Pending:
                questList.Remove(quest.Id);
                questInfo?.Clear();
                break;

            case QuestStatus.Expired:
                questList.Remove(quest.Id);
                break;

            default:
                break;
        }
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

    private void OnSelectAdventure(UnitModel model) => adventurerInfo.SetAdventurer(model);

    private void RefreshQuestList()
    {
        questList.Clear();
        foreach (var q in questRegistry.Available)
            questList.Add(q);
    }
}