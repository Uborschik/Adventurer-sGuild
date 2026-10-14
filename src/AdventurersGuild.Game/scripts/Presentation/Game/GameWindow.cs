namespace AdventurersGuild.Presentation.Game;

using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Application.Adventurers;
using AdventurersGuild.Application.Quests;
using AdventurersGuild.Application.Reports;
using AdventurersGuild.Core;
using AdventurersGuild.Data;
using AdventurersGuild.Data.Adventurer;
using AdventurersGuild.Data.Balance;
using AdventurersGuild.Data.Compendium;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Data.Quest;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Common;
using AdventurersGuild.Domain.Quests;
using AdventurersGuild.Presentation.Adventurers;
using AdventurersGuild.Presentation.Common;
using AdventurersGuild.Presentation.Quests;
using AdventurersGuild.Presentation.Reports;
using Godot;

public partial class GameWindow : Control
{
    private AdventurerRegistry adventurerRegistry;
    private GuildBank bank;
    private GameClock clock;
    private QuestRegistry questRegistry;
    private QuestResolver questResolver;
    private QuestFlow questFlow;
    private ReportsRegistry reportsRegistry;
    private AdventurerFactory adventurerFactory;

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

        AdventurerBalanceLoader.Load();
        QuestBalanceLoader.Load();
        AdventurerLoader.Load();
        QuestLoader.Load();
        CompendiumLoader.Load();
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
        adventurerRegistry = new AdventurerRegistry();
        questRegistry = new QuestRegistry();
        questResolver = new QuestResolver();
        bank = new GuildBank();
        clock = new GameClock(GameTime.Zero);

        adventurerFactory = new AdventurerFactory();

        var generator = new QuestGenerator(QuestDatabase.Phases);
        var blueprints = QuestDatabase.Blueprints.Values.ToList();
        var questFactory = new QuestFactory(generator, blueprints);

        questFlow = new QuestFlow(questRegistry, questFactory, questResolver, clock, bank);

        reportsRegistry = new ReportsRegistry();

        questFlow = new QuestFlow(questRegistry, questFactory, questResolver, clock, bank, reportsRegistry);
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
        questBoardWindow?.Start();
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