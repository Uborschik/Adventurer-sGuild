namespace AdventurersGuild.Editor.BalanceLab;

#if TOOLS
using Godot;

[Tool]
public partial class BalanceLabMain : MarginContainer
{
    private const int margin = 10;

    public override void _Ready()
    {
        AddThemeConstantOverride("margin_left", margin);
        AddThemeConstantOverride("margin_top", margin);
        AddThemeConstantOverride("margin_right", margin);
        AddThemeConstantOverride("margin_bottom", margin);

        var tabs = new TabContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        AddChild(tabs);

        var combatTabScene = GD.Load<PackedScene>("res://addons/balance_lab/tabs/CombatLabTab.tscn");
        var combatTabInstance = combatTabScene.Instantiate<CombatLabTab>();
        combatTabInstance.Name = "Combat Lab";
        tabs.AddChild(combatTabInstance);

        tabs.AddChild(CreatePlaceholderTab("Stat Curves", "Stat curves will be here."));
        tabs.AddChild(CreatePlaceholderTab("Quest Lab", "Quest generator will be here."));
    }

    private static CenterContainer CreatePlaceholderTab(string tabName, string text)
    {
        var centerContainer = new CenterContainer { Name = tabName };
        var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", 20);
        centerContainer.AddChild(label);
        return centerContainer;
    }
}
#endif