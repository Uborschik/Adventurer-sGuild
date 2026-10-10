#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Core;
using AdventurersGuild.Core.Localization;
using AdventurersGuild.Data.Adventurer;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Data.Quest;
using AdventurersGuild.Domain.Adventurers;
using Godot;

[Tool]
public partial class RacesTab : Control
{
    private const string RacesPath = "res://Resources/Data/Adventurer/AdventurerRaces.json";

    private const float LabelW = 130;
    private const float FormWidth = 750;

    private ItemList _list;
    private VBoxContainer _editorRoot;
    private Label _status;

    private LineEdit _idEdit;
    private LineEdit _nameRuEdit, _nameEnEdit;
    private SpinBox _resilienceSpin, _populationSpin;

    private readonly Dictionary<string, SpinBox> _statBonusBoxes = new();

    private VBoxContainer _classWeightsBox;
    private readonly List<ClassWeightRow> _classWeightRows = new();

    private readonly List<AdventurerRaceInfo> _races = new();
    private AdventurerRaceInfo _current;

    private enum StatusKind { Ok, Warning, Error }

    private sealed class ClassWeightRow
    {
        public OptionButton ClassOption;
        public SpinBox WeightSpin;
    }

    public override void _Ready()
    {
        GD.Print("[RacesTab] _Ready вызван");
        ReloadFromDisk();

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

    private void BuildLeft(Control parent)
    {
        var left = new VBoxContainer { CustomMinimumSize = new Vector2(200, 0) };
        left.AddThemeConstantOverride("separation", 4);
        parent.AddChild(left);
        left.AddChild(new Label { Text = "Расы" });

        _list = new ItemList
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 300),
        };
        _list.ItemSelected += OnRaceSelected;
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
        BuildIdentitySection(identityBox);

        var (_, coreBox) = CollapsibleSection(_editorRoot, "Core", expanded: true);
        BuildCoreSection(coreBox);

        var (_, bonusesBox) = CollapsibleSection(_editorRoot, "Stat bonuses", expanded: true);
        BuildStatBonusesSection(bonusesBox);

        var (_, weightsBox) = CollapsibleSection(_editorRoot, "Class weights", expanded: true);
        BuildClassWeightsSection(weightsBox);
    }

    private void BuildIdentitySection(Control parent)
    {
        var idRow = FormRow(parent);
        idRow.AddChild(FormLabel("Id"));
        _idEdit = FormField();
        _idEdit.Editable = false;
        idRow.AddChild(_idEdit);

        var nameRow = FormRow(parent);
        nameRow.AddChild(FormLabel("Name RU"));
        _nameRuEdit = FormField();
        nameRow.AddChild(_nameRuEdit);
        nameRow.AddChild(FormLabel("Name EN"));
        _nameEnEdit = FormField();
        nameRow.AddChild(_nameEnEdit);
    }

    private void BuildCoreSection(Control parent)
    {
        var r1 = FormRow(parent);
        r1.AddChild(FormLabel("Resilience ×"));
        _resilienceSpin = new SpinBox
        {
            MinValue = 0.1,
            MaxValue = 5.0,
            Step = 0.01,
            Value = 1.0,
            CustomMinimumSize = new Vector2(120, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        r1.AddChild(_resilienceSpin);

        r1.AddChild(FormLabel("Population"));
        _populationSpin = new SpinBox
        {
            MinValue = 0,
            MaxValue = 10000,
            Step = 1,
            Value = 0,
            CustomMinimumSize = new Vector2(120, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        r1.AddChild(_populationSpin);
        r1.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
    }

    private void BuildStatBonusesSection(Control parent)
    {
        parent.AddChild(new Label
        {
            Text = "Целочисленные бонусы к статам. Могут быть отрицательными.",
            Modulate = new Color(0.7f, 0.7f, 0.7f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        var grid = new GridContainer { Columns = 3 };
        grid.AddThemeConstantOverride("h_separation", 16);
        grid.AddThemeConstantOverride("v_separation", 4);
        parent.AddChild(grid);

        _statBonusBoxes.Clear();

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
                MinValue = -10,
                MaxValue = 10,
                Step = 1,
                Value = 0,
                CustomMinimumSize = new Vector2(80, 0),
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            };
            cell.AddChild(spin);
            _statBonusBoxes[statId] = spin;
            grid.AddChild(cell);
        }
    }

    private void BuildClassWeightsSection(Control parent)
    {
        parent.AddChild(new Label
        {
            Text = "Веса классов для этой расы. 0 = класс не появляется.",
            Modulate = new Color(0.7f, 0.7f, 0.7f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        _classWeightsBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _classWeightsBox.AddThemeConstantOverride("separation", 4);
        parent.AddChild(_classWeightsBox);

        var addBtn = new Button
        {
            Text = "+ class weight",
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        addBtn.Pressed += () => AddClassWeightRow(null, null);
        parent.AddChild(addBtn);
    }

    private void AddClassWeightRow(string classId, int? weight)
    {
        var row = new ClassWeightRow();

        var h = new HBoxContainer();
        h.AddThemeConstantOverride("separation", 8);

        row.ClassOption = new OptionButton
        {
            CustomMinimumSize = new Vector2(240, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        PopulateClassOptions(row.ClassOption);
        if (!string.IsNullOrEmpty(classId))
            SelectClassOption(row.ClassOption, classId);
        h.AddChild(row.ClassOption);

        row.WeightSpin = new SpinBox
        {
            MinValue = 0,
            MaxValue = 1000,
            Step = 1,
            Value = weight ?? 1,
            CustomMinimumSize = new Vector2(100, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        h.AddChild(row.WeightSpin);

        var rm = new Button
        {
            Text = "×",
            Flat = true,
            CustomMinimumSize = new Vector2(28, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        rm.Pressed += () => { _classWeightRows.Remove(row); h.QueueFree(); };
        h.AddChild(rm);

        _classWeightsBox.AddChild(h);
        _classWeightRows.Add(row);
    }

    private static void PopulateClassOptions(OptionButton option)
    {
        option.Clear();
        foreach (var c in AdventurerDatabase.Classes.Values.OrderBy(x => x.Id))
        {
            string name = c.Name.Get(Loc.Language);
            option.AddItem(string.IsNullOrEmpty(name) ? c.Id : $"{c.Id}  ({name})");
        }
    }

    private static string ReadClassId(OptionButton option)
    {
        if (option.Selected < 0 || option.Selected >= option.ItemCount) return null;
        string text = option.GetItemText(option.Selected);
        int spaceIdx = text.IndexOf("  (", StringComparison.Ordinal);
        return spaceIdx > 0 ? text.Substring(0, spaceIdx) : text;
    }

    private static void SelectClassOption(OptionButton option, string classId)
    {
        for (int i = 0; i < option.ItemCount; i++)
        {
            string text = option.GetItemText(i);
            int spaceIdx = text.IndexOf("  (", StringComparison.Ordinal);
            string id = spaceIdx > 0 ? text.Substring(0, spaceIdx) : text;
            if (id == classId) { option.Selected = i; return; }
        }
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
        foreach (var r in _races)
            _list.AddItem(r.Id);
    }

    private void SelectFirst()
    {
        if (_races.Count == 0) return;
        _list.Select(0);
        OnRaceSelected(0);
    }

    private void OnRaceSelected(long index)
    {
        if (index < 0 || index >= _races.Count) return;
        _current = _races[(int)index];
        LoadCurrentIntoUI();
    }

    private void LoadCurrentIntoUI()
    {
        var r = _current;

        _idEdit.Text = r.Id ?? "";
        _nameRuEdit.Text = r.Name.Ru ?? "";
        _nameEnEdit.Text = r.Name.En ?? "";
        _resilienceSpin.Value = r.ResilienceMultiplier;
        _populationSpin.Value = r.Weight?.Population ?? 0;

        foreach (var statId in StatIds.All)
        {
            int v = 0;
            if (r.StatBonuses != null && r.StatBonuses.TryGetValue(statId, out var b))
                v = b;
            _statBonusBoxes[statId].Value = v;
        }

        foreach (var child in _classWeightsBox.GetChildren())
            child.QueueFree();
        _classWeightRows.Clear();

        var warnings = new List<string>();

        if (r.Weight?.Classes != null)
        {
            foreach (var kv in r.Weight.Classes)
            {
                if (!AdventurerDatabase.Classes.ContainsKey(kv.Key))
                    warnings.Add($"weight.classes: неизвестный класс '{kv.Key}'");
                AddClassWeightRow(kv.Key, kv.Value);
            }
        }

        if (r.Weight?.Classes == null || r.Weight.Classes.Count == 0)
            warnings.Add("weight.classes пуст — раса никогда не появится");

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
                var dict = new Dictionary<string, AdventurerRaceInfo>();
                foreach (var r in _races) dict[r.Id] = r;
                AdventurerDatabase.Install(races: dict);

                string suffix = warnings.Count > 0
                    ? $" Предупреждения: {string.Join("; ", warnings)}"
                    : "";
                SetStatus($"Preview: {_races.Count} рас в памяти (диск не тронут).{suffix}",
                          StatusKind.Warning);
            }
            catch (Exception e)
            {
                SetStatus($"Preview FAILED: {e.Message}", StatusKind.Error);
                GD.PushError($"[RacesTab] Preview: {e}");
            }
            return;
        }

        try
        {
            var db = new AdventurerRaceDatabase { Races = _races };
            JsonWriter.Write(RacesPath, db);
        }
        catch (Exception e)
        {
            SetStatus($"Apply FAILED: {e.Message}", StatusKind.Error);
            GD.PushError($"[RacesTab] Apply: {e}");
            return;
        }

        try { AdventurerDatabase.Load(); QuestDatabase.Load(); }
        catch (Exception e)
        {
            SetStatus($"Записано, но Load упал: {e.Message}", StatusKind.Error);
            GD.PushError($"[RacesTab] Load after apply: {e}");
            return;
        }

        SetStatus(warnings.Count > 0
            ? $"Applied ({_races.Count}). Предупреждения: {string.Join("; ", warnings)}"
            : $"Applied: {_races.Count} рас записано",
            warnings.Count > 0 ? StatusKind.Warning : StatusKind.Ok);
    }

    private void OnRevertPressed()
    {
        ReloadFromDisk();
        RefreshList();
        SelectFirst();
        try
        {
            AdventurerDatabase.Load();
            QuestDatabase.Load();
            SetStatus($"Reverted: {_races.Count} рас перезагружено", StatusKind.Ok);
        }
        catch (Exception e)
        {
            SetStatus($"Revert: Load упал: {e.Message}", StatusKind.Error);
        }
    }

    private List<string> SyncUIToCurrent()
    {
        var warnings = new List<string>();
        var r = _current;

        r.Name = new LocalizedString { Ru = _nameRuEdit.Text, En = _nameEnEdit.Text };
        r.ResilienceMultiplier = NumericHelpers.Round4(_resilienceSpin.Value);

        var bonuses = new Dictionary<string, int>();
        foreach (var statId in StatIds.All)
            bonuses[statId] = (int)_statBonusBoxes[statId].Value;
        r.StatBonuses = bonuses;

        if (r.Weight == null) r.Weight = new RaceWeightInfo();
        r.Weight.Population = (int)_populationSpin.Value;

        var classes = new Dictionary<string, int>();
        var seen = new HashSet<string>();
        foreach (var row in _classWeightRows)
        {
            string cid = ReadClassId(row.ClassOption);
            if (string.IsNullOrEmpty(cid)) continue;
            if (!seen.Add(cid)) warnings.Add($"weight.classes: дубликат '{cid}'");
            classes[cid] = (int)row.WeightSpin.Value;
        }
        r.Weight.Classes = classes;

        return warnings;
    }

    private void ReloadFromDisk()
    {
        try
        {
            var db = JsonLoader.Load<AdventurerRaceDatabase>(RacesPath);
            _races.Clear();
            if (db?.Races != null) _races.AddRange(db.Races);
            GD.Print($"[RacesTab] ReloadFromDisk: загружено {_races.Count} рас");
        }
        catch (Exception e)
        {
            GD.PushError($"[RacesTab] Не удалось загрузить {RacesPath}: {e}");
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