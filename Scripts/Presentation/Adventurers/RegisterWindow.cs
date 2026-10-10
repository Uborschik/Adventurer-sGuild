using AdventurersGuild.Application.Adventurers;
using AdventurersGuild.Core;
using AdventurersGuild.Data.Adventurer;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Presentation.Common;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdventurersGuild.Presentation.Adventurers;

public partial class RegisterWindow : InteractableWindow
{
    private const int CandidateCount = 4;

    private AvailableBoard availableBoard;
    private AdventurerInfo adventurerInfo;
    private AdventurerBoard adventurerBoard;
    private Control control;

    private readonly List<AdventurerModel> candidates = [];
    private AdventurerRegistry adventurerRegistry;
    private AdventurerFactory adventurerFactory;
    private Button hire;
    private Button reject;

    private string selectedId;

    public override void _Ready()
    {
        if (!this.TryGetInstance(out availableBoard)) return;
        if (!this.TryGetInstance(out adventurerInfo)) return;
        if (!this.TryGetInstance(out adventurerBoard)) return;

        control = GetNode<Control>("VBox/Control");

        hire = control.GetNode<Button>("Hire");
        reject = control.GetNode<Button>("Reject");

        availableBoard.AdventurerList.SelectedIdChanged += OnAvailableSelected;
        adventurerBoard.AdventurerList.SelectedIdChanged += OnAdventurerSelected;

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
        availableBoard?.AdventurerList?.Deselect();
        adventurerBoard?.AdventurerList?.Deselect();
    }

    protected override void OnClose()
    {
        availableBoard?.AdventurerList?.Deselect();
        adventurerBoard?.AdventurerList?.Deselect();
    }

    public void Bind(AdventurerRegistry adventurerRegistry, AdventurerFactory adventurerFactory)
    {
        this.adventurerFactory = adventurerFactory;
        this.adventurerRegistry = adventurerRegistry;

        adventurerBoard.Bind(this.adventurerRegistry);
    }

    public void Start()
    {
        TestAllRacesAndClasses();
        GenerateCandidates();

        adventurerBoard.Refresh();
    }

    private void OnAvailableSelected(string id)
    {
        adventurerBoard?.AdventurerList?.Deselect();

        selectedId = id;

        var model = FindModel(id);
        adventurerInfo.SetAdventurer(model);
    }


    private void OnAdventurerSelected(string id)
    {
        availableBoard?.AdventurerList?.Deselect();

        selectedId = null;

        var model = adventurerRegistry.GetById(id);
        adventurerInfo.SetAdventurer(model);
    }

    private void OnRemove(AdventurerModel model)
    {
        adventurerBoard.AdventurerList.Remove(model.Id);
    }

    private void OnHire()
    {
        if (selectedId == null) return;

        var model = FindModel(selectedId);

        if (model == null) return;

        if (adventurerRegistry.TryAdd(model))
        {
            adventurerBoard.AdventurerList.Add(model);
            availableBoard.AdventurerList.Remove(selectedId);

            candidates.Remove(model);

            selectedId = null;
            adventurerInfo.Clear();
        }
    }

    private void OnReject()
    {
        if (selectedId == null) return;

        var model = FindModel(selectedId);

        if (model == null) return;

        availableBoard.AdventurerList.Remove(selectedId);

        candidates.Remove(model);

        selectedId = null;
        adventurerInfo.Clear();
    }

    private AdventurerModel FindModel(string id)
    {
        foreach (var c in candidates)
            if (c.Id == id) return c;
        return null;
    }

    private void GenerateCandidates()
    {
        candidates.Clear();

        for (int i = 0; i < CandidateCount; i++)
        {
            var model = adventurerFactory.Create();
            candidates.Add(model);
            availableBoard.AdventurerList.Add(model);
        }
    }

    private void TestAllRacesAndClasses()
    {
        var classIds = AdventurerDatabase.Classes.Keys.ToList();

        foreach (var classId in classIds)
        {
            var model = adventurerFactory.Create("human", classId, 1);

            if (!adventurerRegistry.TryAdd(model)) continue;
        }
    }
}