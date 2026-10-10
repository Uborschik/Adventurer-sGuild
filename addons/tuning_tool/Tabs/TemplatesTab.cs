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
public partial class TemplatesTab : Control
{
    private const string TemplatesPath = "res://Resources/Data/Quest/QuestTemplates.json";

    private const float LabelW = 130;
    private const float FormWidth = 750;

    private ItemList _list;
    private VBoxContainer _editorRoot;
    private Label _status;

    private LineEdit _idEdit;
    private LineEdit _nameRuEdit, _nameEnEdit;
    private LineEdit _descRuEdit, _descEnEdit;
    private OptionButton _typeOption;
    private SpinBox _difficultySpin;
    private SpinBox _travelDaysSpin;
    private SpinBox _levelMinSpin, _levelMaxSpin;
    private SpinBox _baseGoldSpin, _baseGlorySpin;

    private VBoxContainer _requiresBox;
    private readonly List<OptionButton> _requiresOptions = new();

    private VBoxContainer _creaturesBox;
    private readonly List<OptionButton> _creatureOptions = new();

    private VBoxContainer _locationsBox;
    private readonly List<OptionButton> _locationOptions = new();

    private readonly List<QuestTemplate> _templates = new();
    private QuestTemplate _current;

    private readonly List<string> _typeIds = new();
    private readonly List<string> _markerIds = new();
    private readonly List<string> _creatureIds = new();
    private readonly List<string> _locationIds = new();

    private enum StatusKind { Ok, Warning, Error }

    public override void _Ready()
    {
        GD.Print("[TemplatesTab] _Ready вызван");
        ReloadFromDisk();
        RebuildIdLists();

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

    private void RebuildIdLists()
    {
        _typeIds.Clear();
        foreach (var t in QuestDatabase.Types.Values.OrderBy(x => x.Id))
            _typeIds.Add(t.Id);

        _markerIds.Clear();
        foreach (var p in QuestDatabase.Phases.Values.Where(x => x.IsMarker).OrderBy(x => x.Id))
            _markerIds.Add(p.Id);

        _creatureIds.Clear();
        foreach (var c in QuestDatabase.Creatures.Values.OrderBy(x => x.Id))
            _creatureIds.Add(c.Id);

        _locationIds.Clear();
        foreach (var l in QuestDatabase.Locations.Values.OrderBy(x => x.Id))
            _locationIds.Add(l.Id);
    }

    // ==================== Левая колонка ====================

    private void BuildLeft(Control parent)
    {
        var left = new VBoxContainer { CustomMinimumSize = new Vector2(200, 0) };
        left.AddThemeConstantOverride("separation", 4);
        parent.AddChild(left);
        left.AddChild(new Label { Text = "Шаблоны" });

        _list = new ItemList
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 300),
        };
        _list.ItemSelected += OnTemplateSelected;
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
        var (_, textBox) = CollapsibleSection(_editorRoot, "Text", expanded: true);
        var (_, metaBox) = CollapsibleSection(_editorRoot, "Meta", expanded: true);
        var (_, requiresBox) = CollapsibleSection(_editorRoot, "Requires", expanded: true);
        var (_, creaturesBox) = CollapsibleSection(_editorRoot, "Extra creatures", expanded: false);
        var (_, locationsBox) = CollapsibleSection(_editorRoot, "Locations", expanded: true);

        BuildIdentitySection(identityBox);
        BuildTextSection(textBox);
        BuildMetaSection(metaBox);
        BuildRequiresSection(requiresBox);
        BuildCreaturesSection(creaturesBox);
        BuildLocationsSection(locationsBox);
    }

    private void BuildIdentitySection(Control parent)
    {
        var idRow = FormRow(parent);
        idRow.AddChild(FormLabel("Id"));
        _idEdit = FormField();
        _idEdit.Editable = false;
        idRow.AddChild(_idEdit);

        var typeRow = FormRow(parent);
        typeRow.AddChild(FormLabel("Type"));
        _typeOption = new OptionButton
        {
            CustomMinimumSize = new Vector2(220, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        foreach (var tid in _typeIds) _typeOption.AddItem(tid);
        typeRow.AddChild(_typeOption);
        typeRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
    }

    private void BuildTextSection(Control parent)
    {
        var nameRow = FormRow(parent);
        nameRow.AddChild(FormLabel("Name RU"));
        _nameRuEdit = FormField();
        nameRow.AddChild(_nameRuEdit);
        nameRow.AddChild(FormLabel("Name EN"));
        _nameEnEdit = FormField();
        nameRow.AddChild(_nameEnEdit);

        var descRuRow = FormRow(parent);
        descRuRow.AddChild(FormLabel("Desc RU"));
        _descRuEdit = FormField();
        descRuRow.AddChild(_descRuEdit);

        var descEnRow = FormRow(parent);
        descEnRow.AddChild(FormLabel("Desc EN"));
        _descEnEdit = FormField();
        descEnRow.AddChild(_descEnEdit);

        parent.AddChild(new Label
        {
            Text = "Подстановки в Desc: {creature:nom/gen/acc/ins/pre}, {location:nom/gen/acc/ins/pre}.",
            Modulate = new Color(0.7f, 0.7f, 0.7f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });
    }

    private void BuildMetaSection(Control parent)
    {
        var r1 = FormRow(parent);
        r1.AddChild(FormLabel("Difficulty mod"));
        _difficultySpin = Spin(-2, 2, 0.05, 0);
        r1.AddChild(_difficultySpin);

        r1.AddChild(FormLabel("Travel days"));
        _travelDaysSpin = Spin(0, 365, 1, 0);
        r1.AddChild(_travelDaysSpin);

        var r2 = FormRow(parent);
        r2.AddChild(FormLabel("Level range"));
        r2.AddChild(new Label { Text = "min", VerticalAlignment = VerticalAlignment.Center });
        _levelMinSpin = Spin(1, 1000, 1, 0, 90);
        r2.AddChild(_levelMinSpin);
        r2.AddChild(new Label { Text = "max", VerticalAlignment = VerticalAlignment.Center });
        _levelMaxSpin = Spin(1, 1000, 1, 0, 90);
        r2.AddChild(_levelMaxSpin);

        var r3 = FormRow(parent);
        r3.AddChild(FormLabel("Base gold"));
        _baseGoldSpin = Spin(0, 1000000, 1, 0);
        r3.AddChild(_baseGoldSpin);
        r3.AddChild(FormLabel("Base glory"));
        _baseGlorySpin = Spin(0, 1000000, 1, 0);
        r3.AddChild(_baseGlorySpin);
    }

    private void BuildRequiresSection(Control parent)
    {
        parent.AddChild(new Label
        {
            Text = "Маркеры, которые обязательно должны быть в квесте. " +
                   "Реализацию даёт существо.",
            Modulate = new Color(0.7f, 0.7f, 0.7f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        _requiresBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _requiresBox.AddThemeConstantOverride("separation", 4);
        parent.AddChild(_requiresBox);

        var addBtn = new Button { Text = "+ require", SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
        addBtn.Pressed += () => AddRequireRow(null);
        parent.AddChild(addBtn);
    }

    private void BuildCreaturesSection(Control parent)
    {
        parent.AddChild(new Label
        {
            Text = "Доп. существа сверх тех, что уже есть в локациях квеста.",
            Modulate = new Color(0.7f, 0.7f, 0.7f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        _creaturesBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _creaturesBox.AddThemeConstantOverride("separation", 4);
        parent.AddChild(_creaturesBox);

        var addBtn = new Button { Text = "+ creature", SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
        addBtn.Pressed += () => AddCreatureRow(null);
        parent.AddChild(addBtn);
    }

    private void BuildLocationsSection(Control parent)
    {
        parent.AddChild(new Label
        {
            Text = "Локации, где может происходить квест. Одна выбирается случайно.",
            Modulate = new Color(0.7f, 0.7f, 0.7f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        _locationsBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _locationsBox.AddThemeConstantOverride("separation", 4);
        parent.AddChild(_locationsBox);

        var addBtn = new Button { Text = "+ location", SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
        addBtn.Pressed += () => AddLocationRow(null);
        parent.AddChild(addBtn);
    }

    // ==================== Rows ====================

    private void AddRequireRow(string phaseId)
    {
        AddGenericRow(_requiresBox, _requiresOptions, _markerIds, phaseId);
    }
    private void AddCreatureRow(string creatureId)
    {
        AddGenericRow(_creaturesBox, _creatureOptions, _creatureIds, creatureId);
    }
    private void AddLocationRow(string locationId)
    {
        AddGenericRow(_locationsBox, _locationOptions, _locationIds, locationId);
    }

    private void AddGenericRow(
        VBoxContainer parent,
        List<OptionButton> registry,
        IReadOnlyList<string> ids,
        string currentId)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        var option = new OptionButton
        {
            CustomMinimumSize = new Vector2(240, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        PopulateOptions(option, ids, currentId);
        row.AddChild(option);

        var rm = new Button
        {
            Text = "×",
            Flat = true,
            CustomMinimumSize = new Vector2(28, 0),
        };
        rm.Pressed += () => { registry.Remove(option); row.QueueFree(); };
        row.AddChild(rm);

        parent.AddChild(row);
        registry.Add(option);
    }

    private static void PopulateOptions(OptionButton option, IReadOnlyList<string> ids, string currentId)
    {
        option.Clear();
        foreach (var id in ids)
            option.AddItem(id);

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

    private static SpinBox Spin(double min, double max, double step, double value, float width = 120)
    {
        return new SpinBox
        {
            MinValue = min,
            MaxValue = max,
            Step = step,
            Value = value,
            CustomMinimumSize = new Vector2(width, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
    }

    // ==================== Список ====================

    private void RefreshList()
    {
        _list.Clear();
        foreach (var t in _templates)
            _list.AddItem(t.Id);
    }

    private void SelectFirst()
    {
        if (_templates.Count == 0) return;
        _list.Select(0);
        OnTemplateSelected(0);
    }

    private void OnTemplateSelected(long index)
    {
        if (index < 0 || index >= _templates.Count) return;
        _current = _templates[(int)index];
        LoadCurrentIntoUI();
    }

    private void LoadCurrentIntoUI()
    {
        var t = _current;

        _idEdit.Text = t.Id ?? "";

        int typeIdx = _typeIds.IndexOf(t.Type ?? "");
        _typeOption.Selected = typeIdx >= 0 ? typeIdx : 0;

        _nameRuEdit.Text = t.Name.Ru ?? "";
        _nameEnEdit.Text = t.Name.En ?? "";
        _descRuEdit.Text = t.Description.Ru ?? "";
        _descEnEdit.Text = t.Description.En ?? "";

        _difficultySpin.Value = t.DifficultyModifier;
        _travelDaysSpin.Value = t.TravelDays;
        _levelMinSpin.Value = t.LevelRange?.Min ?? 1;
        _levelMaxSpin.Value = t.LevelRange?.Max ?? 5;
        _baseGoldSpin.Value = t.BaseGold;
        _baseGlorySpin.Value = t.BaseGlory;

        ClearRows(_requiresBox, _requiresOptions);
        if (t.Requires != null) foreach (var p in t.Requires) AddRequireRow(p);

        ClearRows(_creaturesBox, _creatureOptions);
        if (t.Creatures != null) foreach (var c in t.Creatures) AddCreatureRow(c);

        ClearRows(_locationsBox, _locationOptions);
        if (t.Locations != null) foreach (var l in t.Locations) AddLocationRow(l);

        SetStatus("", StatusKind.Ok);
    }

    private static void ClearRows(VBoxContainer box, List<OptionButton> registry)
    {
        foreach (var child in box.GetChildren())
            child.QueueFree();
        registry.Clear();
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
                QuestDatabase.ApplyData(templates: _templates);
                string suffix = warnings.Count > 0
                    ? $" Предупреждения: {string.Join("; ", warnings)}"
                    : "";
                SetStatus($"Preview: {_templates.Count} шаблонов в памяти (диск не тронут).{suffix}",
                          StatusKind.Warning);
            }
            catch (Exception e)
            {
                SetStatus($"Preview FAILED: {e.Message}", StatusKind.Error);
                GD.PushError($"[TemplatesTab] Preview: {e}");
            }
            return;
        }

        try
        {
            var db = new QuestTemplateDatabase { Templates = _templates };
            JsonWriter.Write(TemplatesPath, db);
        }
        catch (Exception e)
        {
            SetStatus($"Apply FAILED: {e.Message}", StatusKind.Error);
            GD.PushError($"[TemplatesTab] Apply: {e}");
            return;
        }

        try { QuestDatabase.Load(); }
        catch (Exception e)
        {
            SetStatus($"Записано, но QuestDatabase.Load упал: {e.Message}", StatusKind.Error);
            GD.PushError($"[TemplatesTab] Load: {e}");
            return;
        }

        SetStatus(warnings.Count > 0
            ? $"Applied ({_templates.Count}). Предупреждения: {string.Join("; ", warnings)}"
            : $"Applied: {_templates.Count} шаблонов записано",
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
            SetStatus($"Reverted: {_templates.Count} шаблонов перезагружено", StatusKind.Ok);
        }
        catch (Exception e)
        {
            SetStatus($"Revert: Load упал: {e.Message}", StatusKind.Error);
        }
    }

    private List<string> SyncUIToCurrent()
    {
        var warnings = new List<string>();
        var t = _current;

        t.Name = new LocalizedString { Ru = _nameRuEdit.Text, En = _nameEnEdit.Text };
        t.Description = new LocalizedString { Ru = _descRuEdit.Text, En = _descEnEdit.Text };

        if (_typeOption.Selected >= 0 && _typeOption.Selected < _typeIds.Count)
            t.Type = _typeIds[_typeOption.Selected];

        t.DifficultyModifier = NumericHelpers.Round4(_difficultySpin.Value);
        t.TravelDays = (int)_travelDaysSpin.Value;

        if (t.LevelRange == null) t.LevelRange = new LevelRangeInfo();
        t.LevelRange.Min = (int)_levelMinSpin.Value;
        t.LevelRange.Max = (int)_levelMaxSpin.Value;
        if (t.LevelRange.Max < t.LevelRange.Min)
            warnings.Add($"levelRange: max < min ({t.LevelRange.Min}..{t.LevelRange.Max})");

        t.BaseGold = (int)_baseGoldSpin.Value;
        t.BaseGlory = (int)_baseGlorySpin.Value;

        // Requires
        var requires = new List<string>();
        var seenR = new HashSet<string>();
        foreach (var opt in _requiresOptions)
        {
            string pid = ReadOptionId(opt);
            if (string.IsNullOrEmpty(pid)) continue;
            if (!seenR.Add(pid)) warnings.Add($"requires: дубликат '{pid}'");
            requires.Add(pid);
        }
        t.Requires = requires;

        // Extra creatures
        var creatures = new List<string>();
        var seenC = new HashSet<string>();
        foreach (var opt in _creatureOptions)
        {
            string cid = ReadOptionId(opt);
            if (string.IsNullOrEmpty(cid)) continue;
            if (!seenC.Add(cid)) warnings.Add($"creatures: дубликат '{cid}'");
            creatures.Add(cid);
        }
        t.Creatures = creatures.Count > 0 ? creatures : null;

        // Locations
        var locations = new List<string>();
        var seenL = new HashSet<string>();
        foreach (var opt in _locationOptions)
        {
            string lid = ReadOptionId(opt);
            if (string.IsNullOrEmpty(lid)) continue;
            if (!seenL.Add(lid)) warnings.Add($"locations: дубликат '{lid}'");
            locations.Add(lid);
        }
        t.Locations = locations;

        return warnings;
    }

    private void ReloadFromDisk()
    {
        try
        {
            var db = JsonLoader.Load<QuestTemplateDatabase>(TemplatesPath);
            _templates.Clear();
            if (db?.Templates != null) _templates.AddRange(db.Templates);
            GD.Print($"[TemplatesTab] ReloadFromDisk: загружено {_templates.Count} шаблонов");
        }
        catch (Exception e)
        {
            GD.PushError($"[TemplatesTab] Не удалось загрузить {TemplatesPath}: {e}");
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