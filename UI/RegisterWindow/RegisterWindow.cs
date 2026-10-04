using Godot;
using System.Collections.Generic;

public partial class RegisterWindow : InteractableWindow
{
    private const int CandidateCount = 4;

    private readonly List<AdventurerModel> candidates = [];

    private AdventurerList adventurerList;
    private AdventurerBoard adventurerBoard;
    private AdventurerFactory factory;

    private Button hire;
    private Button reject;

    private string selectedId;

    public override void _Ready()
    {
        if (!this.TryGetInstance(out adventurerList)) return;

        hire = GetNode<Button>("Control/Buttons/HBox/Hire");
        reject = GetNode<Button>("Control/Buttons/HBox/Reject");

        hire.Pressed += OnHire;
        reject.Pressed += OnReject;
    }

    public override void _ExitTree()
    {
        if (hire != null)
            hire.Pressed -= OnHire;
        if (reject != null)
            reject.Pressed -= OnReject;
    }

    protected override void OnOpen()
    {
        if (adventurerList == null) return;

        adventurerList.Deselect();
        adventurerBoard.AdventurerList.Deselect();
        adventurerList.SelectedIdChanged += OnSelectedIdChanged;
    }

    protected override void OnClose()
    {
        if (adventurerList == null) return;

        adventurerList.Deselect();
        adventurerBoard.AdventurerList.Deselect();
        adventurerList.SelectedIdChanged -= OnSelectedIdChanged;
    }

    public void Bind(AdventurerBoard adventurerBoard, AdventurerFactory factory)
    {
        this.adventurerBoard = adventurerBoard;
        this.factory = factory;
    }

    public void Start()
    {
        GenerateCandidates();
    }

    private void OnSelectedIdChanged(string id)
    {
        selectedId = id;

        var model = FindModel(id);
        adventurerBoard.AdventurerInfo.SetAdventurer(model);
    }

    private void OnHire()
    {
        if (selectedId == null) return;

        var model = FindModel(selectedId);
        if (model == null) return;

        if (!adventurerBoard.TryAdd(model)) return;

        adventurerList.Remove(selectedId);
        candidates.Remove(model);
        selectedId = null;
        adventurerBoard.AdventurerInfo.Clear();
    }

    private void OnReject()
    {
        var model = FindModel(selectedId);
        if (model != null) candidates.Remove(model);

        adventurerList.Remove(selectedId);
        selectedId = null;
        adventurerBoard.AdventurerInfo.Clear();
    }

    private void GenerateCandidates()
    {
        candidates.Clear();
        for (int i = 0; i < CandidateCount; i++)
        {
            var model = factory.Create();
            candidates.Add(model);
            adventurerList.Add(model);
        }
    }

    private AdventurerModel FindModel(string id)
    {
        foreach (var c in candidates)
            if (c.Id == id) return c;
        return null;
    }
}