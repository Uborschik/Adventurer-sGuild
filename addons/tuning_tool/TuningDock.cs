#if TOOLS
using Godot;

[Tool]
public partial class TuningDock : Control
{
    public static bool PreviewEnabled { get; private set; }

    public override void _Ready()
    {
        AutoTranslateMode = AutoTranslateModeEnum.Disabled;
        EnsureGameDataLoaded();

        PreviewEnabled = false;

        var root = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        AddChild(root);
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var header = new HBoxContainer();
        var previewCheck = new CheckBox
        {
            Text = "Preview (в память, без записи на диск)",
            TooltipText =
                "ВКЛ: Apply не трогает JSON — изменения уходят в игровые базы в памяти.\n" +
                "После этого Simulator видит ваши правки без перезапуска.\n" +
                "ВЫКЛ: Apply пишет JSON и пересобирает игровые базы (обычный режим).\n\n" +
                "Revert всегда перечитывает диск — независимо от Preview.",
            ButtonPressed = false,
        };
        previewCheck.Toggled += v => PreviewEnabled = v;
        header.AddChild(previewCheck);
        root.AddChild(header);
        root.AddChild(new HSeparator());

        var tabs = new TabContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        root.AddChild(tabs);
        tabs.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        tabs.TabAlignment = TabBar.AlignmentMode.Center;

        AddTab(tabs, "Simulator", new SimulatorTab());
        AddTab(tabs, "Phases", new PhasesTab());
        AddTab(tabs, "Creatures", new CreaturesTab());
        AddTab(tabs, "Classes", new ClassesTab());
        AddTab(tabs, "Races", new RacesTab());
        AddTab(tabs, "Stats", new StatsTab());
        AddTab(tabs, "Balance", new BalanceTab());
    }

    private static void AddTab(TabContainer tabs, string name, Control content)
    {
        content.Name = name;
        tabs.AddChild(content);
    }

    private static void EnsureGameDataLoaded()
    {
        Loc.Load();
        AdventurerDatabase.Load();
        QuestDatabase.Load();
        QuestBalance.Load();
        AdventurerBalance.Load();
    }
}
#endif