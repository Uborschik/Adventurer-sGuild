#if TOOLS
using Godot;

[Tool]
public partial class TuningDock : Control
{
    public override void _Ready()
    {
        AutoTranslateMode = AutoTranslateModeEnum.Disabled;
        EnsureGameDataLoaded();

        var tabs = new TabContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        AddChild(tabs);
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