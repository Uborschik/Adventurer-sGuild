#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Core;
using AdventurersGuild.Core.Localization;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Data.Quest;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Quests;
using Godot;

[Tool]
public partial class CreaturesTab : Control
{
    private const string CreaturesPath = "res://Resources/Data/Quest/Creatures.json";

    private const float LabelW = 130;
    private const float FormWidth = 750;

    private ItemList _list;
    private VBoxContainer _editorRoot;
    private Label _status;

    private LineEdit _idEdit;
    private LineEdit _nameEnEdit;
    private LineEdit _nomEdit, _genEdit, _datEdit, _accEdit, _insEdit, _preEdit;
    private SpinBox _minLvlSpin, _maxLvlSpin;

    private readonly Dictionary<string, SpinBox> _growthBoxes = new();

    private VBoxContainer _promisesBox;
    private readonly List<PromiseCard> _promiseCards = new();

    private readonly List<CreatureInfo> _creatures = new();
    private CreatureInfo _current;

    private readonly List<string> _markerIds = new();

    private enum StatusKind { Ok, Warning, Error }

    private sealed class PromiseCard
    {
        public OptionButton PhaseOption;
        public SpinBox DurationSpin;
        public SpinBox ExpSpin;
    }

    public override void _Ready()
    {
        GD.Print("[CreaturesTab] _Ready вызван");
        ReloadFromDisk();
        RebuildMarkerIds();

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

    private void RebuildMarkerIds()
    {
        _markerIds.Clear();
        foreach (var p in QuestDatabase.Phases.Values.Where(x => x.IsMarker).OrderBy(x => x.Id))
            _markerIds.Add(p.Id);
    }

    // ==================== Левая колонка ====================

    private void BuildLeft(Control parent)
    {
        var left = new VBoxContainer { CustomMinimumSize = new Vector2(200, 0) };
        left.AddThemeConstantOverride("separation", 4);
        parent.AddChild(left);
        left.AddChild(new Label { Text = "Существа" });

        _list = new ItemList
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 300),
        };
        _list.ItemSelected += OnCreatureSelected;
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
        var (_, growthBox) = CollapsibleSection(_editorRoot, "Growth weights", expanded: true);
        var (_, promisesBox) = CollapsibleSection(_editorRoot, "Promises", expanded: true);

        BuildIdentitySection(identityBox);
        BuildNameSection(nameBox);
        BuildGrowthSection(growthBox);
        BuildPromisesSection(promisesBox);
    }

    private void BuildIdentitySection(Control parent)
    {
        var idRow = FormRow(parent);
        idRow.AddChild(FormLabel("Id"));
        _idEdit = FormField();
        _idEdit.Editable = false;
        idRow.AddChild(_idEdit);

        var lvlRow = FormRow(parent);
        lvlRow.AddChild(FormLabel("Level range"));
        lvlRow.AddChild(new Label { Text = "min", VerticalAlignment = VerticalAlignment.Center });
        _minLvlSpin = Spin(1, 60, 1, 1, 90);
        lvlRow.AddChild(_minLvlSpin);
        lvlRow.AddChild(new Label { Text = "max", VerticalAlignment = VerticalAlignment.Center });
        _maxLvlSpin = Spin(1, 60, 1, 60, 90);
        lvlRow.AddChild(_maxLvlSpin);
        lvlRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
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

        _nomEdit = AddCaseField(grid, "nom", "Именительный. Кто? что?\nПример: волк, гоблин.");
        _genEdit = AddCaseField(grid, "gen", "Родительный. Кого? чего?\nПример: волка, гоблина.");
        _datEdit = AddCaseField(grid, "dat", "Дательный. Кому? чему?\nПример: волку, гоблину.");
        _accEdit = AddCaseField(grid, "acc", "Винительный. Кого? что?\nПример: волка, гоблина.");
        _insEdit = AddCaseField(grid, "ins", "Творительный. Кем? чем?\nПример: волком, гоблином.");
        _preEdit = AddCaseField(grid, "pre", "Предложный. О ком? о чём?\nПример: волке, гоблине.");
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

    private void BuildGrowthSection(Control parent)
    {
        parent.AddChild(new Label
        {
            Text = "Веса роста статов. Формула как у авантюристов: " +
                   "statValue = StatBaseValue + refMax(level) * weight.",
            Modulate = new Color(0.7f, 0.7f, 0.7f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        var grid = new GridContainer { Columns = 3 };
        grid.AddThemeConstantOverride("h_separation", 16);
        grid.AddThemeConstantOverride("v_separation", 4);
        parent.AddChild(grid);

        _growthBoxes.Clear();

        foreach (var statId in StatIds.All)
        {
            var cell = new HBoxContainer();
            cell.AddThemeConstantOverride("separation", 6);
            cell.AddChild(new Label
            {
                Text = statId,
                CustomMinimumSize = new Vector2(32, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Modulate = new Color(0.85f, 0.85f, 0.85f),
            });
            var spin = new SpinBox
            {
                MinValue = 0,
                MaxValue = 10,
                Step = 0.01,
                Value = 0,
                CustomMinimumSize = new Vector2(80, 0),
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            };
            cell.AddChild(spin);
            _growthBoxes[statId] = spin;
            grid.AddChild(cell);
        }
    }

    private void BuildPromisesSection(Control parent)
    {
        parent.AddChild(new Label
        {
            Text = "Обещания: какие маркеры существо предоставляет в квест. " +
                   "Каждое обещание реализуется через статы существа.",
            Modulate = new Color(0.7f, 0.7f, 0.7f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        _promisesBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _promisesBox.AddThemeConstantOverride("separation", 4);
        parent.AddChild(_promisesBox);

        var addBtn = new Button { Text = "+ promise", SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
        addBtn.Pressed += () => AddPromiseCard(null);
        parent.AddChild(addBtn);
    }

    private void AddPromiseCard(PhasePromise promise)
    {
        var card = new PromiseCard();

        var frame = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var inner = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        inner.AddThemeConstantOverride("separation", 6);
        frame.AddChild(inner);

        var h = new HBoxContainer();
        h.AddThemeConstantOverride("separation", 8);

        h.AddChild(new Label
        {
            Text = "phase",
            CustomMinimumSize = new Vector2(64, 0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            AutoTranslateMode = AutoTranslateModeEnum.Disabled,
        });

        card.PhaseOption = new OptionButton
        {
            CustomMinimumSize = new Vector2(200, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        PopulatePhaseOptions(card.PhaseOption, promise?.Phase);
        h.AddChild(card.PhaseOption);

        h.AddChild(new Label
        {
            Text = "duration",
            CustomMinimumSize = new Vector2(70, 0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
        });
        card.DurationSpin = Spin(0, 10000, 1, promise?.DurationMinutes ?? 0, 100);
        h.AddChild(card.DurationSpin);

        h.AddChild(new Label
        {
            Text = "exp",
            CustomMinimumSize = new Vector2(40, 0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
        });
        card.ExpSpin = Spin(0, 10000, 1, promise?.ExpReward ?? 0, 90);
        h.AddChild(card.ExpSpin);

        var rm = new Button
        {
            Text = "×",
            Flat = true,
            CustomMinimumSize = new Vector2(28, 0),
        };
        rm.Pressed += () => { _promiseCards.Remove(card); frame.QueueFree(); };
        h.AddChild(rm);
        inner.AddChild(h);

        _promisesBox.AddChild(frame);
        _promiseCards.Add(card);
    }

    private void PopulatePhaseOptions(OptionButton option, string currentId)
    {
        option.Clear();
        foreach (var pid in _markerIds)
            option.AddItem(pid);

        if (string.IsNullOrEmpty(currentId)) { option.Selected = 0; return; }

        for (int i = 0; i < option.ItemCount; i++)
        {
            if (option.GetItemText(i) == currentId) { option.Selected = i; return; }
        }

        option.AddItem($"{currentId}  (неизвестный)");
        option.Selected = option.ItemCount - 1;
    }

    private static string ReadPhaseId(OptionButton option)
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
        foreach (var c in _creatures)
            _list.AddItem(c.Id);
    }

    private void SelectFirst()
    {
        if (_creatures.Count == 0) return;
        _list.Select(0);
        OnCreatureSelected(0);
    }

    private void OnCreatureSelected(long index)
    {
        if (index < 0 || index >= _creatures.Count) return;
        _current = _creatures[(int)index];
        LoadCurrentIntoUI();
    }

    private void LoadCurrentIntoUI()
    {
        var c = _current;

        _idEdit.Text = c.Id ?? "";
        _minLvlSpin.Value = c.MinLvl;
        _maxLvlSpin.Value = c.MaxLvl;

        var n = c.Name;
        _nomEdit.Text = n?.Ru?.Nom ?? "";
        _genEdit.Text = n?.Ru?.Gen ?? "";
        _datEdit.Text = n?.Ru?.Dat ?? "";
        _accEdit.Text = n?.Ru?.Acc ?? "";
        _insEdit.Text = n?.Ru?.Ins ?? "";
        _preEdit.Text = n?.Ru?.Pre ?? "";
        _nameEnEdit.Text = n?.En ?? "";

        foreach (var statId in StatIds.All)
        {
            double v = 0;
            if (c.GrowthWeights != null && c.GrowthWeights.TryGetValue(statId, out var w))
                v = w;
            _growthBoxes[statId].Value = v;
        }

        foreach (var child in _promisesBox.GetChildren())
            child.QueueFree();
        _promiseCards.Clear();
        if (c.Promises != null)
            foreach (var p in c.Promises)
                AddPromiseCard(p);

        SetStatus("", StatusKind.Ok);
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
                var dict = new Dictionary<string, CreatureInfo>();
                foreach (var c in _creatures) dict[c.Id] = c;
                QuestDatabase.ApplyData(creatures: dict);

                string suffix = warnings.Count > 0
                    ? $" Предупреждения: {string.Join("; ", warnings)}"
                    : "";
                SetStatus($"Preview: {_creatures.Count} существ в памяти (диск не тронут).{suffix}",
                          StatusKind.Warning);
            }
            catch (Exception e)
            {
                SetStatus($"Preview FAILED: {e.Message}", StatusKind.Error);
                GD.PushError($"[CreaturesTab] Preview: {e}");
            }
            return;
        }

        try
        {
            var db = new CreatureDatabase { Creatures = _creatures };
            JsonWriter.Write(CreaturesPath, db);
        }
        catch (Exception e)
        {
            SetStatus($"Apply FAILED: {e.Message}", StatusKind.Error);
            GD.PushError($"[CreaturesTab] Apply: {e}");
            return;
        }

        try { QuestDatabase.Load(); }
        catch (Exception e)
        {
            SetStatus($"Записано, но QuestDatabase.Load упал: {e.Message}", StatusKind.Error);
            GD.PushError($"[CreaturesTab] Load: {e}");
            return;
        }

        SetStatus(warnings.Count > 0
            ? $"Applied ({_creatures.Count}). Предупреждения: {string.Join("; ", warnings)}"
            : $"Applied: {_creatures.Count} существ записано",
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
            SetStatus($"Reverted: {_creatures.Count} существ перезагружено", StatusKind.Ok);
        }
        catch (Exception e)
        {
            SetStatus($"Revert: Load упал: {e.Message}", StatusKind.Error);
        }
    }

    private List<string> SyncUIToCurrent()
    {
        var warnings = new List<string>();
        var c = _current;

        if (c.Name == null) c.Name = new LocalizedNoun();
        if (c.Name.Ru == null) c.Name.Ru = new NounForms();
        c.Name.Ru.Nom = EmptyToNull(_nomEdit.Text);
        c.Name.Ru.Gen = EmptyToNull(_genEdit.Text);
        c.Name.Ru.Dat = EmptyToNull(_datEdit.Text);
        c.Name.Ru.Acc = EmptyToNull(_accEdit.Text);
        c.Name.Ru.Ins = EmptyToNull(_insEdit.Text);
        c.Name.Ru.Pre = EmptyToNull(_preEdit.Text);
        c.Name.En = _nameEnEdit.Text;

        c.MinLvl = (int)_minLvlSpin.Value;
        c.MaxLvl = (int)_maxLvlSpin.Value;
        if (c.MaxLvl < c.MinLvl)
            warnings.Add($"levelRange: max < min ({c.MinLvl}..{c.MaxLvl})");

        var weights = new Dictionary<string, double>();
        foreach (var statId in StatIds.All)
            weights[statId] = NumericHelpers.Round4(_growthBoxes[statId].Value);
        c.GrowthWeights = weights;

        var promises = new List<PhasePromise>();
        var seen = new HashSet<string>();
        foreach (var card in _promiseCards)
        {
            string pid = ReadPhaseId(card.PhaseOption);
            if (string.IsNullOrEmpty(pid)) continue;
            if (!seen.Add(pid))
                warnings.Add($"promises: дубликат '{pid}'");

            promises.Add(new PhasePromise
            {
                Phase = pid,
                DurationMinutes = (int)card.DurationSpin.Value,
                ExpReward = (int)card.ExpSpin.Value,
            });
        }
        c.Promises = promises;

        return warnings;
    }

    private static string EmptyToNull(string s) => string.IsNullOrEmpty(s) ? null : s;

    private void ReloadFromDisk()
    {
        try
        {
            var db = JsonLoader.Load<CreatureDatabase>(CreaturesPath);
            _creatures.Clear();
            if (db?.Creatures != null) _creatures.AddRange(db.Creatures);
            GD.Print($"[CreaturesTab] ReloadFromDisk: загружено {_creatures.Count} существ");
        }
        catch (Exception e)
        {
            GD.PushError($"[CreaturesTab] Не удалось загрузить {CreaturesPath}: {e}");
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