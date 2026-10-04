using System;
using Godot;

public partial class Slot : Control
{
    public event Action<int> LeftClicked;
    public event Action<int> RightClicked;

    public int Index { get; set; }

    private TextureRect icon;
    private NinePatchRect frame;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;

        icon = GetNodeOrNull<TextureRect>("Icon");
        frame = GetNodeOrNull<NinePatchRect>("Frame");
        if (icon != null) icon.MouseFilter = MouseFilterEnum.Ignore;
        if (frame != null) frame.MouseFilter = MouseFilterEnum.Ignore;

        frame.Visible = true;

        TooltipText = "";
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mb || !mb.Pressed) return;

        if (mb.ButtonIndex == MouseButton.Left)
        {
            LeftClicked?.Invoke(Index);
            AcceptEvent();
        }
        else if (mb.ButtonIndex == MouseButton.Right)
        {
            RightClicked?.Invoke(Index);
            AcceptEvent();
        }
    }

    public void SetContent(AdventurerModel data, Texture2D tex)
    {
        if (icon != null) icon.Texture = data != null ? tex : null;
        TooltipText = data?.FullName ?? "";
    }

    public void SetFrameState(bool state) => frame.Visible = state;

    public void Clear() => SetContent(null, null);
}