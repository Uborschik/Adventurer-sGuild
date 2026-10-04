using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Party : Control
{
    private const int MaxPartySize = 4;

    public event Action<AdventurerModel> SelectedAdventurer;
    public event Action PartyChanged;

    private Button moveLeftBtn;
    private Button addBtn;
    private Button moveRightBtn;
    private Slot[] slots;

    private readonly AdventurerModel[] party = new AdventurerModel[MaxPartySize];
    private int selectedSlot = -1;

    public Button AddBtn => addBtn;
    public int Count
    {
        get
        {
            var count = 0;
            foreach (var p in party) if (p != null) count++;
            return count;
        }
    }

    public override void _Ready()
    {
        moveLeftBtn = GetNode<Button>("Buttons/MoveLeft");
        addBtn = GetNode<Button>("Buttons/Add");
        moveRightBtn = GetNode<Button>("Buttons/MoveRight");

        slots = [.. this.GetAllInstances<Slot>()];

        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            slot.Index = i;

            slot.LeftClicked += OnSlotLeftClicked;
            slot.RightClicked += OnSlotRightClicked;
        }

        moveLeftBtn.Pressed += OnMoveLeftPressed;
        moveRightBtn.Pressed += OnMoveRightPressed;
    }

    public override void _ExitTree()
    {

        if (slots != null)
        {
            foreach (var slot in slots)
            {
                if (slot == null) continue;
                slot.LeftClicked -= OnSlotLeftClicked;
                slot.RightClicked -= OnSlotRightClicked;
            }
        }

        if (moveLeftBtn != null) moveLeftBtn.Pressed -= OnMoveLeftPressed;
        if (moveRightBtn != null) moveRightBtn.Pressed -= OnMoveRightPressed;
    }

    public void Add(AdventurerModel model)
    {
        if (model == null) return;
        if (model.StatusId != StatusIds.Free) return;
        if (party.Contains(model)) return;

        var free = FirstFreeSlot();
        if (free < 0) return;

        party[free] = model;
        RefreshSlot(free);

        model.SetStatus(StatusIds.InParty);
        NotifyChanged();
    }

    public List<AdventurerModel> BuildParty()
    {
        var list = new List<AdventurerModel>();
        foreach (var p in party) if (p != null) list.Add(p);
        return list;
    }

    public void DropBusy()
    {
        var changed = false;

        for (int i = 0; i < party.Length; i++)
        {
            if (party[i] == null) continue;
            if (AdventurerDatabase.IsFree(party[i])) continue;

            party[i].SetStatus(StatusIds.Free);
            party[i] = null;
            slots[i].Clear();
            changed = true;
        }

        if (selectedSlot >= 0 && party[selectedSlot] == null) selectedSlot = -1;

        RefreshHighlight();
        RefreshArrows();

        if (changed) NotifyChanged();
    }

    public void ClearParty()
    {
        for (int i = 0; i < party.Length; i++)
        {
            if (party[i] != null && party[i].StatusId == StatusIds.InParty)
                party[i].SetStatus(StatusIds.Free);

            party[i] = null;
            slots[i].Clear();
        }

        selectedSlot = -1;

        RefreshHighlight();
        RefreshArrows();

        NotifyChanged();
    }

    private void OnSlotLeftClicked(int index)
    {
        if (party[index] == null) return;

        selectedSlot = (selectedSlot == index) ? -1 : index;

        var model = party[index];
        SelectedAdventurer?.Invoke(model);

        RefreshHighlight();
        RefreshArrows();

        NotifyChanged();
    }

    private void OnSlotRightClicked(int index)
    {
        if (party[index] == null) return;

        var model = party[index];

        party[index] = null;
        slots[index].Clear();

        if (selectedSlot == index) selectedSlot = -1;

        model.SetStatus(StatusIds.Free);

        SelectedAdventurer?.Invoke(null);

        RefreshHighlight();
        RefreshArrows();

        NotifyChanged();
    }

    private void OnMoveLeftPressed() => MoveSelected(-1);
    private void OnMoveRightPressed() => MoveSelected(+1);

    private void MoveSelected(int direction)
    {
        if (selectedSlot < 0) return;

        var target = selectedSlot + direction;

        if (target < 0 || target >= MaxPartySize) return;

        (party[selectedSlot], party[target]) = (party[target], party[selectedSlot]);

        RefreshSlot(selectedSlot);
        RefreshSlot(target);

        selectedSlot = target;

        RefreshHighlight();
        RefreshArrows();

        NotifyChanged();
    }

    private void NotifyChanged() => PartyChanged?.Invoke();

    private void RefreshSlot(int index)
    {
        var data = party[index];
        if (data == null) slots[index].Clear();
        else slots[index].SetContent(data, AdventurerDatabase.ClassIcon(data.ClassId));
    }

    private void RefreshHighlight()
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i].SetFrameState(i != selectedSlot);

        RefreshArrows();
    }

    private void RefreshArrows()
    {
        moveLeftBtn.Disabled = selectedSlot <= 0;
        moveRightBtn.Disabled = selectedSlot < 0 || selectedSlot >= MaxPartySize - 1;
    }


    private int FirstFreeSlot()
    {
        for (int i = 0; i < party.Length; i++)
            if (party[i] == null) return i;
        return -1;
    }
}
