using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class GameWindow : Control
{
#if DEBUG
    // private DebugResolvedTable debugTable;
#endif
    // === Models ===
    private AdventurerRegistry adventurerRegistry;
    private GuildBank bank;
    private GameClock clock;
    private QuestRegistry questRegistry;
    private QuestResolver questResolver;
    private QuestFlow questFlow;
    private AdventurerFactory adventurerFactory;

    // === Views ===
    private Status statusBar;
    private AdventurerBoard adventurerBoard;
    private Navigation navigation;
    private RegisterWindow registerWindow;
    private QuestBoardWindow questBoardWindow;
    private PartySelect partySelect;

    public GameWindow()
    {
        Loc.Load();
        Loc.SetLanguage("ru");

        Log.InfoSink = GD.Print;
        Log.WarnSink = GD.PushWarning;
        Log.ErrorSink = GD.PushError;

        AdventurerBalance.Load();
        QuestBalance.Load();
        AdventurerDatabase.Load();
        QuestDatabase.Load();
    }

    public override void _Ready()
    {
        CreateModels();

        if (!CollectViews()) return;

        BindViews();

        navigation.Setup();

#if DEBUG
        // if (debugTable == null)
        // {
        //     var scene = GD.Load<PackedScene>("res://UI/Debug/DebugResolvedTable.tscn");
        //     debugTable = scene.Instantiate<DebugResolvedTable>();
        //     AddChild(debugTable);
        // }

        // debugTable?.Bind(questRegistry);
#endif

        TestAllRacesAndClasses();
        StartWindows();

        clock.TimeAdvanced += adventurerRegistry.Tick;
    }

    public override void _ExitTree()
    {
        clock.TimeAdvanced -= adventurerRegistry.Tick;

        questFlow?.Stop();
    }

    // === Инициализация моделей ===

    private void CreateModels()
    {
        adventurerRegistry = new AdventurerRegistry();
        questRegistry = new QuestRegistry();
        questResolver = new QuestResolver();
        bank = new GuildBank();
        clock = new GameClock(GameTime.Zero);

        adventurerFactory = new AdventurerFactory();
        var questFactory = new QuestFactory();
        questFlow = new QuestFlow(questRegistry, questFactory, questResolver, clock, bank);
    }

    // === Сбор View-узлов ===

    private bool CollectViews()
    {
        if (!this.TryGetInstance(out statusBar)) return Fail("Status");
        if (!this.TryGetInstance(out adventurerBoard)) return Fail("AdventurerBoard");
        if (!this.TryGetInstance(out navigation)) return Fail("Navigation");

        // Опциональные — не критичны для запуска
        this.TryGetInstance(out registerWindow);
        this.TryGetInstance(out questBoardWindow);
        this.TryGetInstance(out partySelect);
        return true;
    }

    private static bool Fail(string what)
    {
        GD.PushError($"GameWindow: не найден {what}.");
        return false;
    }

    // === Связка View с моделями ===

    private void BindViews()
    {
        statusBar.Bind(bank, clock);

        adventurerBoard.Bind(adventurerRegistry);
        registerWindow?.Bind(adventurerBoard, adventurerFactory);
        questBoardWindow?.Bind(adventurerBoard, questRegistry, questFlow);

        partySelect?.Bind(
            adventurerRegistry, questFlow, questResolver);

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
    {
        return navigation.GetAllInstances<NavigationButton>();
    }

    private List<InteractableWindow> GetInteractableWindows()
    {
        var root = GetNodeOrNull<Control>("Windows");
        return root == null ? [] : root.GetAllInstances<InteractableWindow>();
    }

    private static InteractableWindow FindWindow(List<InteractableWindow> windows, WindowType type)
    {
        foreach (var w in windows)
            if (w.Type == type) return w;
        return null;
    }

    // === Стартовые действия ===

    private void TestAllRacesAndClasses()
    {
        var raceIds = AdventurerDatabase.Races.Keys.ToList();
        var classIds = AdventurerDatabase.Classes.Keys.ToList();

        foreach (var raceId in raceIds)
        {
            Log.Info($"--- {raceId} ---");
            foreach (var classId in classIds)
            {
                adventurerBoard.TryAdd(adventurerFactory.Create(raceId, classId, 1));
            }
        }
    }

    private void StartWindows()
    {
        registerWindow?.Start();
        questFlow?.Start();
        questBoardWindow?.Start();
    }

    // === Debug ===

#if DEBUG
    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey key) return;
        if (!key.Pressed || key.Echo) return;

        switch (key.Keycode)
        {
            case Key.F1:
                clock.Advance(GameTime.FromDays(1));
                GD.Print($"День: {clock.Now}");
                break;

            case Key.F2:
                clock.Advance(GameTime.FromDays(7));
                GD.Print($"Неделя: {clock.Now}");
                break;

            case Key.F3:
                clock.Advance(GameTime.FromMinutes(15));
                GD.Print($"Минут: {clock.Now}");
                break;

            case Key.F9:
                int drawCalls = (int)Performance.GetMonitor(
                    Performance.Monitor.RenderTotalDrawCallsInFrame);
                GD.Print($"Draw Calls: {drawCalls}");
                break;

            case Key.F12:
                // debugTable?.Toggle();
                break;
        }
    }
#endif
}