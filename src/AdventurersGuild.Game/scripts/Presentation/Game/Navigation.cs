using AdventurersGuild.Core;
using AdventurersGuild.Presentation.Common;
using Godot;

namespace AdventurersGuild.Presentation.Game;

public partial class Navigation : Control
{
    private readonly SingleSelection<NavigationButton> selection = new();
    private bool initialized;

    public void Setup()
    {
        if (initialized) return;
        initialized = true;

        var btns = this.GetAllInstances<NavigationButton>();

        if (btns.Count == 0)
        {
            GD.PushError("[Navigation] Не найдено ни одной SidebarButton.");
            return;
        }

        foreach (var btn in btns)
        {
            if (btn.Window == WindowType.None) continue;

            btn.Toggled += pressed => OnButtonToggled(btn, pressed);
        }
    }

    private void OnButtonToggled(NavigationButton btn, bool pressed) => selection.OnToggled(btn, pressed);
}