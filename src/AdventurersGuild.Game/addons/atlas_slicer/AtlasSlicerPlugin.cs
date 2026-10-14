#if TOOLS
using Godot;

[Tool]
public partial class AtlasSlicerPlugin : EditorPlugin
{
    private EditorDock editorDock;
    private AtlasSlicerDock dock;

    public override void _EnterTree()
    {
        if (editorDock != null) return;

        dock = new AtlasSlicerDock { Name = "AtlasSlicerDock" };

        editorDock = new EditorDock
        {
            Title = "Atlas Slicer",
            DefaultSlot = EditorDock.DockSlot.RightUl,
            CustomMinimumSize = new Vector2(280, 0)
        };
        editorDock.AddChild(dock);

        AddDock(editorDock);
    }

    public override void _ExitTree()
    {
        if (editorDock != null)
        {
            RemoveDock(editorDock);
            editorDock.QueueFree();
            editorDock = null;
            dock = null;
        }
    }
}
#endif