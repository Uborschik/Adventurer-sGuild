#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Core;
using AdventurersGuild.Core.Localization;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Data.Quest;
using AdventurersGuild.Domain.Quests;
using Godot;

[Tool]
public partial class LocationsTab : Control
{
    private const string LocationsPath = "res://Resources/Data/Quest/Locations.json";

    private const float LabelW = 130;
    private const float FormWidth = 750;

    private ItemList _list;
    private VBoxContainer _editorRoot;
    private Label _status;

    private LineEdit _idEdit;
    private LineEdit _nameEnEdit;
    private LineEdit _nomEdit, _genEdit, _datEdit, _accEdit, _insEdit, _preEdit;
    private SpinBox _difficultySpin;

    private VBoxContainer _optionalPhasesBox;
    private readonly List<OptionButton> _optionalPhaseOptions = new();

    private VBoxContainer _creaturesBox;
    private readonly List<OptionButton> _creatureOptions = new();

    private readonly List<LocationInfo> _locations = new();
    private LocationInfo _current;

    private readonly List<string> _phaseIds = new();
    private readonly List<string> _creatureIds = new();

    private enum StatusKind { Ok, Warning, Error }

    public override void _Ready()
    {
        GD.Print("[LocationsTab] _Ready вызван");
        ReloadFromDisk();
        RebuildPhaseIds();
        RebuildCreatureIds();

        var main = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        main.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(main);

        BuildLeft(main);
        BuildRight(main);

        RefreshList();
        SelectFirst();
    }

    private void RebuildPhaseIds()
    {
        _phaseIds.Clear();
        foreach (var p in QuestDatabase.Phases.Values.OrderBy(x => x.Id))
            _phaseIds.Add(p.Id);
    }

    private void RebuildCreatureIds()
    {
        _creatureIds.Clear();
        foreach (var c in QuestDatabase.Creatures.Values.OrderBy(x => x.Id))
            _creatureIds.Add(c.Id);
    }

    // ==================== Левая колонка ====================

    private void BuildLeft(Control parent)
    {
        var left = new VBoxContainer { CustomMinimumSize = new Vector2(200, 0) };
        left.AddThemeConstantOverride("separation", 4);
        parent.AddChild(left);
        left.AddChild(new Label { Text = "Локации" });

        _list = new ItemList
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 300),
        };
        _list.ItemSelected += OnLocationSelected;
        left.AddChild(_list);

        var btnRow = new HBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkEnd };
        var applyBtn = new Button { Text = "Apply", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var revertBtn = new Button { Text = "Revert", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        applyBtn.Pressed += OnApplyPressed;
        revertBtn.Pressed += OnRevertPressed;
        btnRow.AddChild(applyBtn);
        btnRow.AddChild(revertBtn);
        left.AddChild(btnRow);

        _status = new Label
        {
            Text = "",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsVertical = SizeFlags.ShrinkEnd,
        };
        left.AddChild(_status);
    }

    // ==================== Правая колонка ====================

    private void BuildRight(Control parent)
    {
        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        parent.AddChild(scroll);

        _editorRoot = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _editorRoot.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(_editorRoot);

        var (_, identityBox) = CollapsibleSection(_editorRoot, "Identity", expanded: true);
        var (_, nameBox) = CollapsibleSection(_editorRoot, "Name", expanded: true);
        var (_, phasesBox) = CollapsibleSection(_editorRoot, "Optional phases", expanded: false);
        var (_, creaturesBox) = CollapsibleSection(_editorRoot, "Creatures", expanded: true);

        BuildIdentitySection(identityBox);
        BuildNameSection(nameBox);
        BuildOptionalPhasesSection(phasesBox);
        BuildCreaturesSection(creaturesBox);
    }

    private void BuildIdentitySection(Control parent)
    {
        var idRow = FormRow(parent);
        idRow.AddChild(FormLabel("Id"));
        _idEdit = FormField();
        _idEdit.Editable = false;
        idRow.AddChild(_idEdit);

        var diffRow = FormRow(parent);
        diffRow.AddChild(FormLabel("Difficulty mod"));
        _difficultySpin = new SpinBox
        {
            MinValue = -2,
            MaxValue = 2,
            Step = 0.05,
            Value = 0,
            CustomMinimumSize = new Vector2(120, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            TooltipText = "Прибавляется к dcDelta всех фаз квеста в этой локации.",
        };
        diffRow.AddChild(_difficultySpin);
        diffRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
    }

    private void BuildNameSection(Control parent)
    {
        var r1 = FormRow(parent);
        r1.AddChild(FormLabel("Name EN"));
        _nameEnEdit = FormField();
        r1.AddChild(_nameEnEdit);

        parent.AddChild(new Label
        {
            Text = "Русские падежи:",
            Modulate = new Color(0.7f, 0.7f, 0.7f),
        });

        var grid = new GridContainer { Columns = 3 };
        grid.AddThemeConstantOverride("h_separation", 16);
        grid.AddThemeConstantOverride("v_separation", 4);
        parent.AddChild(grid);

        _nomEdit = AddCaseField(grid, "nom",
            "Именительный падеж. Что?\nПример: лес, горы, болота.");
        _genEdit = AddCaseField(grid, "gen",
            "Родительный падеж. Чего?\nПример: леса, гор, болот.");
        _datEdit = AddCaseField(grid, "dat",
            "Дательный падеж. Чему?\nПример: лесу, горам, болотам.\n" +
            "Не используется в текущих шаблонах описаний, можно оставить пустым.");
        _accEdit = AddCaseField(grid, "acc",
            "Винительный падеж. Что?\nПример: лес, горы, болота.");
        _insEdit = AddCaseField(grid, "ins",
            "Творительный падеж. Чем?\nПример: лесом, горами, болотами.");
        _preEdit = AddCaseField(grid, "pre",
            "Предложный падеж. О чём? В чём?\nПример: лесу, горах, болотах.");
    }

    private LineEdit AddCaseField(GridContainer grid, string label, string tooltip)
    {
        var cell = new HBoxContainer();
        cell.AddThemeConstantOverride("separation", 6);
        cell.AddChild(new Label
        {
            Text = label,
            CustomMinimumSize = new Vector2(32, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Modulate = new Color(0.85f, 0.85f, 0.85f),
            TooltipText = tooltip,
        });
        var edit = new LineEdit
        {
            CustomMinimumSize = new Vector2(160, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        cell.AddChild(edit);
        grid.AddChild(cell);
        return edit;
    }

    private void BuildOptionalPhasesSection(Control parent)
    {
        parent.AddChild(new Label
        {
            Text = "Фазы, которые могут появиться в этой локации (пока не используется резолвером).",
            Modulate = new Color(0.7f, 0.7f, 0.7f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        _optionalPhasesBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _optionalPhasesBox.AddThemeConstantOverride("separation", 4);
        parent.AddChild(_optionalPhasesBox);

        var addBtn = new Button
        {
            Text = "+ phase",
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        addBtn.Pressed += () => AddOptionalPhaseRow(null);
        parent.AddChild(addBtn);
    }

    private void BuildCreaturesSection(Control parent)
    {
        parent.AddChild(new Label
        {
            Text = "Существа, которые могут обитать в этой локации.",
            Modulate = new Color(0.7f, 0.7f, 0.7f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        _creaturesBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _creaturesBox.AddThemeConstantOverride("separation", 4);
        parent.AddChild(_creaturesBox);

        var addBtn = new Button
        {
            Text = "+ creature",
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        addBtn.Pressed += () => AddCreatureRow(null);
        parent.AddChild(addBtn);
    }

    private void AddOptionalPhaseRow(string phaseId)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        var option = new OptionButton
        {
            CustomMinimumSize = new Vector2(220, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        PopulatePhaseOptions(option, phaseId);
        row.AddChild(option);

        var rm = new Button
        {
            Text = "×",
            Flat = true,
            CustomMinimumSize = new Vector2(28, 0),
        };
        rm.Pressed += () =>
        {
            _optionalPhaseOptions.Remove(option);
            row.QueueFree();
        };
        row.AddChild(rm);

        _optionalPhasesBox.AddChild(row);
        _optionalPhaseOptions.Add(option);
    }

    private void AddCreatureRow(string creatureId)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        var option = new OptionButton
        {
            CustomMinimumSize = new Vector2(220, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        PopulateCreatureOptions(option, creatureId);
        row.AddChild(option);

        var rm = new Button
        {
            Text = "×",
            Flat = true,
            CustomMinimumSize = new Vector2(28, 0),
        };
        rm.Pressed += () =>
        {
            _creatureOptions.Remove(option);
            row.QueueFree();
        };
        row.AddChild(rm);

        _creaturesBox.AddChild(row);
        _creatureOptions.Add(option);
    }

    private void PopulatePhaseOptions(OptionButton option, string currentId)
    {
        option.Clear();
        option.AddItem("(none)");
        foreach (var pid in _phaseIds)
            option.AddItem(pid);

        if (string.IsNullOrEmpty(currentId)) { option.Selected = 0; return; }

        for (int i = 0; i < option.ItemCount; i++)
        {
            if (option.GetItemText(i) == currentId) { option.Selected = i; return; }
        }

        option.AddItem($"{currentId}  (неизвестный)");
        option.Selected = option.ItemCount - 1;
    }

    private void PopulateCreatureOptions(OptionButton option, string currentId)
    {
        option.Clear();
        option.AddItem("(none)");
        foreach (var cid in _creatureIds)
            option.AddItem(cid);

        if (string.IsNullOrEmpty(currentId)) { option.Selected = 0; return; }

        for (int i = 0; i < option.ItemCount; i++)
        {
            if (option.GetItemText(i) == currentId) { option.Selected = i; return; }
        }

        option.AddItem($"{currentId}  (неизвестный)");
        option.Selected = option.ItemCount - 1;
    }

    private static string ReadOptionId(OptionButton option)
    {
        if (option.Selected < 0 || option.Selected >= option.ItemCount) return null;
        string text = option.GetItemText(option.Selected);
        if (text == "(none)") return null;
        int spaceIdx = text.IndexOf("  (неизвестный)", StringComparison.Ordinal);
        return spaceIdx > 0 ? text.Substring(0, spaceIdx) : text;
    }

    // ==================== UI-хелперы ====================

    private static (Button toggle, VBoxContainer content) CollapsibleSection(
        VBoxContainer parent, string title, bool expanded)
    {
        var toggle = new Button
        {
            Text = (expanded ? "▼  " : "▶  ") + title,
            ToggleMode = true,
            ButtonPressed = expanded,
            Flat = true,
            Alignment = HorizontalAlignment.Left,
            FocusMode = Control.FocusModeEnum.None,
            AutoTranslateMode = AutoTranslateModeEnum.Disabled,
        };
        parent.AddChild(toggle);

        var content = new VBoxContainer { Visible = expanded };
        content.AddThemeConstantOverride("separation", 4);
        parent.AddChild(content);

        toggle.Toggled += pressed =>
        {
            toggle.Text = (pressed ? "▼  " : "▶  ") + title;
            content.Visible = pressed;
        };

        return (toggle, content);
    }

    private static HBoxContainer FormRow(Control parent)
    {
        var wrap = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        parent.AddChild(wrap);

        var row = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(FormWidth, 0),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
        };
        row.AddThemeConstantOverride("separation", 8);
        wrap.AddChild(row);

        return row;
    }

    private static Label FormLabel(string text)
    {
        return new Label
        {
            Text = text,
            CustomMinimumSize = new Vector2(LabelW, 0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            AutoTranslateMode = AutoTranslateModeEnum.Disabled,
        };
    }

    private static LineEdit FormField()
    {
        return new LineEdit
        {
            CustomMinimumSize = new Vector2(120, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
    }

    // ==================== Список ====================

    private void RefreshList()
    {
        _list.Clear();
        foreach (var l in _locations)
            _list.AddItem(l.Id);
    }

    private void SelectFirst()
    {
        if (_locations.Count == 0) return;
        _list.Select(0);
        OnLocationSelected(0);
    }

    private void OnLocationSelected(long index)
    {
        if (index < 0 || index >= _locations.Count) return;
        _current = _locations[(int)index];
        LoadCurrentIntoUI();
    }

    private void LoadCurrentIntoUI()
    {
        var l = _current;

        _idEdit.Text = l.Id ?? "";
        _difficultySpin.Value = l.DifficultyModifier;

        var n = l.Name;
        _nomEdit.Text = n?.Ru?.Nom ?? "";
        _genEdit.Text = n?.Ru?.Gen ?? "";
        _datEdit.Text = n?.Ru?.Dat ?? "";
        _accEdit.Text = n?.Ru?.Acc ?? "";
        _insEdit.Text = n?.Ru?.Ins ?? "";
        _preEdit.Text = n?.Ru?.Pre ?? "";
        _nameEnEdit.Text = n?.En ?? "";

        // Optional phases
        foreach (var child in _optionalPhasesBox.GetChildren())
            child.QueueFree();
        _optionalPhaseOptions.Clear();
        if (l.Standalone != null)
            foreach (var pid in l.Standalone)
                AddOptionalPhaseRow(pid);

        // Creatures
        foreach (var child in _creaturesBox.GetChildren())
            child.QueueFree();
        _creatureOptions.Clear();
        if (l.Creatures != null)
            foreach (var cid in l.Creatures)
                AddCreatureRow(cid);

        var warnings = new List<string>();
        if (string.IsNullOrEmpty(n?.Ru?.Nom)) warnings.Add("нет русского name.nom");
        if (string.IsNullOrEmpty(n?.En)) warnings.Add("нет name.en");

        SetStatus(warnings.Count > 0 ? string.Join("; ", warnings) : "",
                  warnings.Count > 0 ? StatusKind.Warning : StatusKind.Ok);
    }

    // ==================== Apply / Revert ====================

    private void OnApplyPressed()
    {
        if (_current == null) { SetStatus("Ничего не выбрано", StatusKind.Error); return; }

        var warnings = SyncUIToCurrent();

        if (TuningDock.PreviewEnabled)
        {
            try
            {
                var dict = new Dictionary<string, LocationInfo>();
                foreach (var l in _locations) dict[l.Id] = l;
                QuestDatabase.ApplyData(locations: dict);

                string suffix = warnings.Count > 0
                    ? $" Предупреждения: {string.Join("; ", warnings)}"
                    : "";
                SetStatus($"Preview: {_locations.Count} локаций в памяти (диск не тронут).{suffix}",
                          StatusKind.Warning);
            }
            catch (Exception e)
            {
                SetStatus($"Preview FAILED: {e.Message}", StatusKind.Error);
                GD.PushError($"[LocationsTab] Preview: {e}");
            }
            return;
        }

        try
        {
            var db = new LocationDatabase { Locations = _locations };
            JsonWriter.Write(LocationsPath, db);
        }
        catch (Exception e)
        {
            SetStatus($"Apply FAILED: {e.Message}", StatusKind.Error);
            GD.PushError($"[LocationsTab] Apply: {e}");
            return;
        }

        try { QuestDatabase.Load(); }
        catch (Exception e)
        {
            SetStatus($"Записано, но QuestDatabase.Load упал: {e.Message}", StatusKind.Error);
            GD.PushError($"[LocationsTab] Load: {e}");
            return;
        }

        SetStatus(warnings.Count > 0
            ? $"Applied ({_locations.Count}). Предупреждения: {string.Join("; ", warnings)}"
            : $"Applied: {_locations.Count} локаций записано",
            warnings.Count > 0 ? StatusKind.Warning : StatusKind.Ok);
    }

    private void OnRevertPressed()
    {
        ReloadFromDisk();
        RefreshList();
        SelectFirst();
        try
        {
            QuestDatabase.Load();
            SetStatus($"Reverted: {_locations.Count} локаций перезагружено", StatusKind.Ok);
        }
        catch (Exception e)
        {
            SetStatus($"Revert: Load упал: {e.Message}", StatusKind.Error);
        }
    }

    private List<string> SyncUIToCurrent()
    {
        var warnings = new List<string>();
        var l = _current;

        if (l.Name == null) l.Name = new LocalizedNoun();
        if (l.Name.Ru == null) l.Name.Ru = new NounForms();

        l.Name.Ru.Nom = EmptyToNull(_nomEdit.Text);
        l.Name.Ru.Gen = EmptyToNull(_genEdit.Text);
        l.Name.Ru.Dat = EmptyToNull(_datEdit.Text);
        l.Name.Ru.Acc = EmptyToNull(_accEdit.Text);
        l.Name.Ru.Ins = EmptyToNull(_insEdit.Text);
        l.Name.Ru.Pre = EmptyToNull(_preEdit.Text);
        l.Name.En = _nameEnEdit.Text;

        if (string.IsNullOrEmpty(l.Name.Ru.Nom))
            warnings.Add("пустой name.nom");
        if (string.IsNullOrEmpty(l.Name.En))
            warnings.Add("пустой name.en");

        l.DifficultyModifier = NumericHelpers.Round4(_difficultySpin.Value);

        // Optional phases
        var optional = new List<string>();
        var seenP = new HashSet<string>();
        foreach (var opt in _optionalPhaseOptions)
        {
            string pid = ReadOptionId(opt);
            if (string.IsNullOrEmpty(pid)) continue;
            if (!seenP.Add(pid)) warnings.Add($"optional phases: дубликат '{pid}'");
            optional.Add(pid);
        }
        l.Standalone = optional.Count > 0 ? optional : null;

        // Creatures
        var creatures = new List<string>();
        var seenC = new HashSet<string>();
        foreach (var opt in _creatureOptions)
        {
            string cid = ReadOptionId(opt);
            if (string.IsNullOrEmpty(cid)) continue;
            if (!seenC.Add(cid)) warnings.Add($"creatures: дубликат '{cid}'");
            creatures.Add(cid);
        }
        l.Creatures = creatures.Count > 0 ? creatures : null;

        return warnings;
    }

    private static string EmptyToNull(string s) => string.IsNullOrEmpty(s) ? null : s;

    private void ReloadFromDisk()
    {
        try
        {
            var db = JsonLoader.Load<LocationDatabase>(LocationsPath);
            _locations.Clear();
            if (db?.Locations != null) _locations.AddRange(db.Locations);
            GD.Print($"[LocationsTab] ReloadFromDisk: загружено {_locations.Count} локаций");
        }
        catch (Exception e)
        {
            GD.PushError($"[LocationsTab] Не удалось загрузить {LocationsPath}: {e}");
        }
        _current = null;
    }

    private void SetStatus(string text, StatusKind kind)
    {
        _status.Text = text;
        _status.Modulate = kind switch
        {
            StatusKind.Ok => new Color(0.6f, 1f, 0.6f),
            StatusKind.Warning => new Color(1f, 0.9f, 0.4f),
            StatusKind.Error => new Color(1f, 0.45f, 0.45f),
            _ => new Color(1f, 1f, 1f),
        };
    }
}
#endif