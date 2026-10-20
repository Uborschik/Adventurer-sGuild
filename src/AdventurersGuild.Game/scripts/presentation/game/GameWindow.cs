namespace AdventurersGuild.Presentation.Game;

using System.Collections.Generic;
using AdventurersGuild.Application.Adventurers;
using AdventurersGuild.Application.Quests;
using AdventurersGuild.Application.Reports;
using AdventurersGuild.Core;
using AdventurersGuild.Data;
using AdventurersGuild.Data.Units;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Data.Quests;
using AdventurersGuild.Domain.Units;
using AdventurersGuild.Domain.Common;
using AdventurersGuild.Domain.Quests;
using AdventurersGuild.Presentation.Adventurers;
using AdventurersGuild.Presentation.Common;
using AdventurersGuild.Presentation.Quests;
using AdventurersGuild.Presentation.Reports;
using Godot;
using AdventurersGuild.Data.Rules;
using System;

public partial class GameWindow : Control
{
    private UnitRegistry adventurerRegistry;
    private GuildBank bank;
    private GameClock clock;
    private QuestRegistry questRegistry;
    private QuestResolver questResolver;
    private QuestFlow questFlow;
    private ReportsRegistry reportsRegistry;
    private UnitFactory adventurerFactory;

    private Status statusBar;
    private Navigation navigation;
    private RegisterWindow registerWindow;
    private QuestBoardWindow questBoardWindow;
    private ReportsWindow reportsWindow;

    public GameWindow()
    {
        FileSystem.Current = new SystemFileSystem();

        LocLoader.Load();
        Loc.SetLanguage("ru");

        Log.InfoSink = GD.Print;
        Log.WarnSink = GD.PushWarning;
        Log.ErrorSink = GD.PushError;

        GameRulesLoader.Load();
        UnitLoader.Load();
        QuestLoader.Load();
    }

    public override void _Ready()
    {
        CreateModels();

        if (!CollectViews()) return;

        BindViews();
        navigation.Setup();
        StartWindows();

        clock.TimeAdvanced += adventurerRegistry.Tick;
    }

    public override void _ExitTree()
    {
        clock.TimeAdvanced -= adventurerRegistry.Tick;
        questFlow?.Stop();
    }

    private void CreateModels()
    {
        adventurerRegistry = new UnitRegistry();
        questRegistry = new QuestRegistry();
        bank = new GuildBank();
        clock = new GameClock(GameTime.Zero);
        reportsRegistry = new ReportsRegistry();

        adventurerFactory = new UnitFactory();

        var questFactory = new QuestFactory();
        questResolver = new QuestResolver();
        var questBoard = new QuestBoard(questRegistry, questFactory);
        var questEncounterResolver = new QuestEncounterResolver(new Random());

        questFlow = new QuestFlow(reportsRegistry, questRegistry, questBoard, questEncounterResolver, clock, bank);
    }

    private bool CollectViews()
    {
        if (!this.TryGetInstance(out statusBar)) return Fail("Status");
        if (!this.TryGetInstance(out navigation)) return Fail("Navigation");
        if (!this.TryGetInstance(out registerWindow)) return Fail("Register");
        if (!this.TryGetInstance(out questBoardWindow)) return Fail("Quests");
        if (!this.TryGetInstance(out reportsWindow)) return Fail("Reports");
        return true;
    }

    private static bool Fail(string what)
    {
        GD.PushError($"[GameWindow] не найден {what}.");
        return false;
    }

    private void BindViews()
    {
        statusBar.Bind(bank, clock);
        registerWindow?.Bind(adventurerRegistry, adventurerFactory);
        questBoardWindow?.Bind(adventurerRegistry, questRegistry, questFlow, questResolver);
        reportsWindow?.Bind(reportsRegistry);
        BindNavigationButtons();
    }

    private void BindNavigationButtons()
    {
        var buttons = GetNavigationButtons();
        var windows = GetInteractableWindows();

        foreach (var btn in buttons)
        {
            if (btn.Window == WindowType.None) continue;

            var window = FindWindow(windows, btn.Window);
            if (window == null)
            {
                GD.PushWarning($"Кнопка {btn.Name}: нет окна типа {btn.Window}.");
                btn.Disabled = true;
                continue;
            }

            btn.Bind(window);
        }
    }

    private List<NavigationButton> GetNavigationButtons()
        => navigation.GetAllInstances<NavigationButton>();

    private List<InteractableWindow> GetInteractableWindows()
    {
        var root = GetNodeOrNull<Control>("VBox/Windows");
        return root == null ? [] : root.GetAllInstances<InteractableWindow>();
    }

    private static InteractableWindow FindWindow(List<InteractableWindow> windows, WindowType type)
    {
        foreach (var w in windows)
            if (w.Type == type) return w;
        return null;
    }

    private void StartWindows()
    {
        registerWindow?.Start();
        questFlow?.Start();
    }

#if DEBUG
    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey key) return;
        if (!key.Pressed || key.Echo) return;

        switch (key.Keycode)
        {
            case Key.F1: clock.Advance(GameTime.FromDays(1)); GD.Print($"День: {clock.Now}"); break;
            case Key.F2: clock.Advance(GameTime.FromDays(7)); GD.Print($"Неделя: {clock.Now}"); break;
            case Key.F3: clock.Advance(GameTime.FromMinutes(15)); GD.Print($"Минут: {clock.Now}"); break;
            case Key.F9:
                int drawCalls = (int)Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
                GD.Print($"Draw Calls: {drawCalls}");
                break;
        }
    }
#endif
}