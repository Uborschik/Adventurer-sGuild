#if TOOLS
using System;
using Godot;

[Tool]
public partial class AtlasSlicerDock : VBoxContainer
{
    private const int ModePng = 0;
    private const int ModeAtlasTexture = 1;
    private const int BrowseButtonWidth = 64;
    private const int FieldHeight = 24;

    private const string DefaultAtlasFolder = "res://Resources/Sprites/Atlases/";
    private const string DefaultOutputPath = "res://Resources/Sprites/";
    private const string DefaultPrefix = "icon_";
    private const int DefaultTileSize = 16;

    private string atlasPathValue = "";
    private string outputPathValue = DefaultOutputPath;

    private LineEdit atlasEdit;
    private LineEdit outputEdit;
    private LineEdit prefix;
    private SpinBox tileSize;
    private SpinBox fromIndex;
    private SpinBox toIndex;
    private CheckBox sliceAllCheck;
    private OptionButton modeSelect;
    private AtlasPreview preview;
    private Label status;
    private FileDialog openDialog;
    private FileDialog dirDialog;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 6);

        BuildUI();
        BuildDialogs();
    }

    // === UI ===

    private void BuildUI()
    {
        // === Atlas ===
        AddChild(MakeLabel("Atlas:"));
        atlasEdit = MakePathField("(no atlas selected)");
        atlasEdit.TextChanged += text => atlasPathValue = text;
        AddChild(MakeBrowseRow(atlasEdit, () =>
            OpenFileDialog(FileDialog.FileModeEnum.OpenFile)));

        // === Output mode ===
        AddChild(MakeLabel("Output mode:"));
        modeSelect = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        modeSelect.AddItem("AtlasTexture (.tres)", ModeAtlasTexture);
        modeSelect.AddItem("Separate PNGs", ModePng);
        modeSelect.Selected = 0;
        AddChild(modeSelect);

        // === Tile size ===
        AddChild(MakeLabel("Tile size (px):"));
        tileSize = new SpinBox
        {
            MinValue = 1,
            MaxValue = 256,
            Step = 1,
            Value = DefaultTileSize,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        tileSize.ValueChanged += _ => preview?.SetTileSize((int)tileSize.Value);
        AddChild(tileSize);

        // === Range ===
        sliceAllCheck = new CheckBox
        {
            Text = "Slice all tiles",
            ButtonPressed = true
        };
        sliceAllCheck.Toggled += pressed => SetRangeEnabled(!pressed);
        AddChild(sliceAllCheck);

        var rangeRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 32)
        };
        AddChild(rangeRow);

        fromIndex = new SpinBox
        {
            MinValue = 1,
            MaxValue = 9999,
            Step = 1,
            Value = 1,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Suffix = "from"
        };
        rangeRow.AddChild(fromIndex);

        toIndex = new SpinBox
        {
            MinValue = 1,
            MaxValue = 9999,
            Step = 1,
            Value = 1,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Suffix = "to"
        };
        rangeRow.AddChild(toIndex);

        SetRangeEnabled(false);

        // === Prefix ===
        AddChild(MakeLabel("File name prefix:"));
        prefix = new LineEdit
        {
            Text = DefaultPrefix,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        AddChild(prefix);

        // === Output folder ===
        AddChild(MakeLabel("Output folder:"));
        outputEdit = MakePathField(DefaultOutputPath);
        outputEdit.Text = outputPathValue;
        outputEdit.TextChanged += text => outputPathValue = text;
        AddChild(MakeBrowseRow(outputEdit, () =>
            OpenFileDialog(FileDialog.FileModeEnum.OpenDir)));

        // === Preview ===
        AddChild(MakeSpacer(12));

        preview = new AtlasPreview
        {
            CustomMinimumSize = new Vector2(0, 180),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(preview);

        AddChild(MakeSpacer(8));

        // === Actions ===
        var actionRow = new HBoxContainer();
        AddChild(actionRow);

        var sliceBtn = new Button { Text = "Slice", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        sliceBtn.Pressed += OnSlicePressed;
        actionRow.AddChild(sliceBtn);

        var resetBtn = new Button
        {
            Text = "Reset",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            AutoTranslateMode = AutoTranslateModeEnum.Disabled
        };
        resetBtn.Pressed += OnResetPressed;
        actionRow.AddChild(resetBtn);

        var openFolderBtn = new Button { Text = "Open folder" };
        openFolderBtn.Pressed += OnOpenFolderPressed;
        actionRow.AddChild(openFolderBtn);

        // === Status ===
        status = new Label
        {
            Text = "Ready.",
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        AddChild(status);
    }

    private void SetRangeEnabled(bool enabled)
    {
        if (fromIndex != null) fromIndex.Editable = enabled;
        if (toIndex != null) toIndex.Editable = enabled;
    }

    private static LineEdit MakePathField(string placeholder) => new()
    {
        Editable = true,
        SizeFlagsHorizontal = SizeFlags.ExpandFill,
        SizeFlagsVertical = SizeFlags.Fill,
        PlaceholderText = placeholder,
        TooltipText = placeholder
    };

    private static HBoxContainer MakeBrowseRow(LineEdit edit, Action onBrowse)
    {
        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 32)
        };
        row.AddChild(edit);

        var btn = new Button
        {
            Text = "...",
            SizeFlagsVertical = SizeFlags.Fill,
            CustomMinimumSize = new Vector2(BrowseButtonWidth, FieldHeight)
        };
        btn.Pressed += onBrowse;
        row.AddChild(btn);

        return row;
    }

    private static Label MakeLabel(string text) => new() { Text = text };

    private static Control MakeSpacer(int height) =>
        new() { CustomMinimumSize = new Vector2(0, height) };

    // === Dialogs ===

    private void BuildDialogs()
    {
        openDialog = new FileDialog
        {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.OpenFile,
            UseNativeDialog = true,
            Size = new Vector2I(700, 500)
        };
        openDialog.AddFilter("*.png", "PNG images");
        openDialog.FileSelected += OnFileSelected;
        AddChild(openDialog);
        openDialog.CurrentDir = ProjectSettings.GlobalizePath(DefaultAtlasFolder);

        dirDialog = new FileDialog
        {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.OpenDir,
            UseNativeDialog = true,
            Size = new Vector2I(700, 500)
        };
        dirDialog.DirSelected += OnDirSelected;
        AddChild(dirDialog);
        dirDialog.CurrentDir = ProjectSettings.GlobalizePath(outputPathValue.TrimEnd('/'));
    }

    private void OpenFileDialog(FileDialog.FileModeEnum mode)
    {
        var dialog = mode == FileDialog.FileModeEnum.OpenFile ? openDialog : dirDialog;
        dialog.PopupCentered(new Vector2I(700, 500));
    }

    private void OnFileSelected(string path)
    {
        atlasPathValue = path;
        atlasEdit.Text = path;
        atlasEdit.TooltipText = path;
        openDialog.CurrentDir = ProjectSettings.GlobalizePath(path.GetBaseDir());
        LoadPreview(path);
    }

    private void OnDirSelected(string path)
    {
        outputPathValue = path;
        outputEdit.Text = path;
        outputEdit.TooltipText = path;
        dirDialog.CurrentDir = ProjectSettings.GlobalizePath(path);
    }

    private void LoadPreview(string path)
    {
        var texture = GD.Load<Texture2D>(path);
        if (texture == null)
        {
            status.Text = "Failed to load preview.";
            return;
        }

        preview.SetTexture(texture);
        preview.SetTileSize((int)tileSize.Value);
        status.Text = $"Loaded: {texture.GetWidth()}×{texture.GetHeight()}";
    }

    // === Slicing ===

    private void OnSlicePressed()
    {
        if (string.IsNullOrEmpty(atlasPathValue))
        {
            status.Text = "No atlas selected.";
            return;
        }

        var image = LoadAtlasImage();
        if (image == null)
        {
            status.Text = "Failed to load atlas.";
            return;
        }

        int size = (int)tileSize.Value;
        int cols = image.GetWidth() / size;
        int rows = image.GetHeight() / size;

        if (cols == 0 || rows == 0)
        {
            status.Text = "Atlas is smaller than tile size.";
            return;
        }

        if (!TryResolveRange(cols * rows, out int from, out int to, out string rangeError))
        {
            status.Text = rangeError;
            return;
        }

        string outputDir = outputPathValue.TrimEnd('/');
        string filePrefix = string.IsNullOrEmpty(prefix.Text) ? DefaultPrefix : prefix.Text;

        EnsureDir(outputDir);

        int saved = modeSelect.GetSelectedId() == ModePng
            ? AtlasSlicer.SliceToPng(image, outputDir, filePrefix, size, cols, rows, from, to)
            : AtlasSlicer.SliceToAtlasTextures(GD.Load<Texture2D>(atlasPathValue), image,
                outputDir, filePrefix, size, cols, rows, from, to);

        EditorInterface.Singleton.GetResourceFilesystem().Scan();
        status.Text = $"Done: {saved} icons in {outputDir}";
        GD.Print($"[AtlasSlicer] {saved} icons → {outputDir} ({GetModeName()}, range {from}-{to})");
    }

    private void OnResetPressed()
    {
        atlasPathValue = "";
        outputPathValue = DefaultOutputPath;

        atlasEdit.Text = "";
        atlasEdit.TooltipText = "(no atlas selected)";

        outputEdit.Text = outputPathValue;
        outputEdit.TooltipText = outputPathValue;

        prefix.Text = DefaultPrefix;
        tileSize.Value = DefaultTileSize;
        modeSelect.Selected = 0;

        sliceAllCheck.ButtonPressed = true;
        fromIndex.Value = 1;
        toIndex.Value = 1;
        SetRangeEnabled(false);

        preview.SetTexture(null);

        openDialog.CurrentDir = ProjectSettings.GlobalizePath(DefaultAtlasFolder);
        dirDialog.CurrentDir = ProjectSettings.GlobalizePath(outputPathValue.TrimEnd('/'));

        status.Text = "Reset. Ready.";
    }

    // Диапазон в UI — от 1. Внутри — от 0.
    //   slice-all включён  → весь атлас
    //   from = 1, to = 1  → только ячейка 1
    //   from = N, to = 1  → от N до конца
    //   from = 1, to = M  → от начала до M
    //   from = N, to = M  → от N до M
    private bool TryResolveRange(int total, out int from, out int to, out string error)
    {
        error = "";

        if (sliceAllCheck.ButtonPressed)
        {
            from = 0;
            to = total - 1;
            return true;
        }

        int fromInput = (int)fromIndex.Value - 1;
        int toInput = (int)toIndex.Value - 1;

        // to = 1 (UI) и from != 1 (UI) → «до конца»
        if (toInput == 0 && fromInput > 0)
            toInput = total - 1;

        from = Mathf.Max(0, fromInput);
        to = Mathf.Min(total - 1, toInput);

        if (from > to)
        {
            error = $"Invalid range: from ({from + 1}) > to ({to + 1}).";
            return false;
        }

        return true;
    }

    private Image LoadAtlasImage()
    {
        var texture = GD.Load<Texture2D>(atlasPathValue);
        return texture?.GetImage();
    }

    private string GetModeName() =>
        modeSelect.GetSelectedId() == ModePng ? "PNG" : "AtlasTexture";

    private static void EnsureDir(string resPath)
    {
        string globalDir = ProjectSettings.GlobalizePath(resPath);
        if (!DirAccess.DirExistsAbsolute(globalDir))
            DirAccess.MakeDirRecursiveAbsolute(globalDir);
    }

    private void OnOpenFolderPressed()
    {
        if (string.IsNullOrEmpty(outputPathValue)) return;

        string globalPath = ProjectSettings.GlobalizePath(outputPathValue);
        if (!DirAccess.DirExistsAbsolute(globalPath))
        {
            status.Text = "Folder not created yet. Slice first.";
            return;
        }

        OS.ShellShowInFileManager(globalPath);
    }
}
#endif