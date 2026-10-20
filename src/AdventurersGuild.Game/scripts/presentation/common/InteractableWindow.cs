using Godot;

namespace AdventurersGuild.Presentation.Common;

public enum WindowType
{
    None,
    Guild,
    Quests,
    Tavern,
    Storage,
    Reports,
    Actions,
    Register
}

public partial class InteractableWindow : Control
{
    [Export] public WindowType Type { get; set; } = WindowType.None;

    public void SetState(bool state)
    {
        if (state) Open();
        else Close();
    }

    private void Open()
    {
        Visible = true;
        OnOpen();
    }
    private void Close()
    {
        Visible = false;
        OnClose();
    }

    protected virtual void OnOpen() { }
    protected virtual void OnClose() { }
}