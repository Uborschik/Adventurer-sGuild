#if TOOLS
using System;
using System.Collections.Generic;
using Godot;

[Tool]
public partial class RacesTab : Control
{
    private const string RacesPath = "res://Resources/Data/Adventurer/AdventurerRaces.json";

    private ItemList _list;
    private VBoxContainer _editorRoot;
    private VBoxContainer _bonusesBox;
    private VBoxContainer _classWeightsBox;
    private Label _status;

    private LineEdit _idEdit, _nameRuEdit, _nameEnEdit;
    private SpinBox _resilienceSpin, _populationSpin;

    private readonly List<AdventurerRaceInfo> _races = new();
    private AdventurerRaceInfo _current;

    private sealed class BonusRow
    {
        public LineEdit Stat;
        public SpinBox Value;
    }
    private sealed class ClassWeightRow
    {
        public LineEdit ClassId;
        public SpinBox Weight;
    }

    private readonly List<BonusRow> _bonusRows = new();
    private readonly List<ClassWeightRow> _classWeightRows = new();

    private enum StatusKind { Ok, Warning, Error }

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
        scroll.AddChild(_editorRoot);

        _idEdit = AddTextRow(_editorRoot, "Id");
        _idEdit.Editable = false;

        _nameRuEdit = AddTextRow(_editorRoot, "Name RU");
        _nameEnEdit = AddTextRow(_editorRoot, "Name EN");

        _resilienceSpin = SpinRow(_editorRoot, "Resilience x",
            0.1, 5.0, 0.01, 1.0,
            "Множитель устойчивости. Влияет на шанс получить рану/умереть.");

        _editorRoot.AddChild(new HSeparator());
        _editorRoot.AddChild(new Label { Text = "Stat bonuses" });
        _editorRoot.AddChild(new Label
        {
            Text = "Целочисленные бонусы к статам. Могут быть отрицательными.",
            Modulate = new Color(0.7f, 0.7f, 0.7f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        _bonusesBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _editorRoot.AddChild(_bonusesBox);
        var addBonusBtn = new Button { Text = "+ stat bonus" };
        addBonusBtn.Pressed += () => AddBonusRow(null, null);
        _editorRoot.AddChild(addBonusBtn);

        _editorRoot.AddChild(new HSeparator());
        _editorRoot.AddChild(new Label { Text = "Weight" });

        _populationSpin = SpinRow(_editorRoot, "Population",
            0, 10000, 1, 0,
            "Вес расы при генерации. Чем больше — тем чаще.");

        _editorRoot.AddChild(new Label { Text = "Class weights" });
        _editorRoot.AddChild(new Label
        {
            Text = "Веса классов для этой расы. 0 = класс не появляется. Отсутствие ключа — тоже 0.",
            Modulate = new Color(0.7f, 0.7f, 0.7f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        _classWeightsBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _editorRoot.AddChild(_classWeightsBox);
        var addClassBtn = new Button { Text = "+ class weight" };
        addClassBtn.Pressed += () => AddClassWeightRow(null, null);
        _editorRoot.AddChild(addClassBtn);
    }

    private static LineEdit AddTextRow(Control parent, string label, string tooltip = null)
    {
        var row = new HBoxContainer();
        row.AddChild(new Label
        {
            Text = label,
            CustomMinimumSize = new Vector2(180, 0),
            TooltipText = tooltip ?? "",
        });
        var e = new LineEdit { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(e);
        parent.AddChild(row);
        return e;
    }

    private static SpinBox SpinRow(Control parent, string label, double min, double max, double step,
                                   double value, string tooltip = null)
    {
        var row = new HBoxContainer();
        row.AddChild(new Label
        {
            Text = label,
            CustomMinimumSize = new Vector2(180, 0),
            TooltipText = tooltip ?? "",
        });
        var s = new SpinBox
        {
            MinValue = min,
            MaxValue = max,
            Step = step,
            Value = value,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        row.AddChild(s);
        parent.AddChild(row);
        return s;
    }

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

        foreach (var child in _bonusesBox.GetChildren()) child.QueueFree();
        _bonusRows.Clear();
        if (r.StatBonuses != null)
            foreach (var kv in r.StatBonuses)
                AddBonusRow(kv.Key, kv.Value);

        _populationSpin.Value = r.Weight?.Population ?? 0;

        foreach (var child in _classWeightsBox.GetChildren()) child.QueueFree();
        _classWeightRows.Clear();
        if (r.Weight?.Classes != null)
            foreach (var kv in r.Weight.Classes)
                AddClassWeightRow(kv.Key, kv.Value);
    }

    private void AddBonusRow(string statId, int? value)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var statEdit = new LineEdit
        {
            Text = statId ?? "",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            PlaceholderText = "stat id (str, dex, ...)",
        };
        var valSpin = new SpinBox
        {
            MinValue = -10,
            MaxValue = 10,
            Step = 1,
            Value = value ?? 0,
            CustomMinimumSize = new Vector2(100, 0),
        };
        var rm = new Button { Text = "X" };
        var br = new BonusRow { Stat = statEdit, Value = valSpin };
        rm.Pressed += () => { _bonusRows.Remove(br); row.QueueFree(); };

        row.AddChild(statEdit);
        row.AddChild(valSpin);
        row.AddChild(rm);
        _bonusesBox.AddChild(row);
        _bonusRows.Add(br);
    }

    private void AddClassWeightRow(string classId, int? weight)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var classEdit = new LineEdit
        {
            Text = classId ?? "",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            PlaceholderText = "class id (warrior, ...)",
        };
        var weightSpin = new SpinBox
        {
            MinValue = 0,
            MaxValue = 1000,
            Step = 1,
            Value = weight ?? 0,
            CustomMinimumSize = new Vector2(100, 0),
        };
        var rm = new Button { Text = "X" };
        var cr = new ClassWeightRow { ClassId = classEdit, Weight = weightSpin };
        rm.Pressed += () => { _classWeightRows.Remove(cr); row.QueueFree(); };

        row.AddChild(classEdit);
        row.AddChild(weightSpin);
        row.AddChild(rm);
        _classWeightsBox.AddChild(row);
        _classWeightRows.Add(cr);
    }

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

        // Обычный путь — без изменений
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

        if (warnings.Count > 0)
        {
            SetStatus($"Applied ({_races.Count}). Предупреждения: {string.Join("; ", warnings)}",
                      StatusKind.Warning);
        }
        else
        {
            SetStatus($"Applied: {_races.Count} рас записано", StatusKind.Ok);
        }
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
        var seenB = new HashSet<string>();
        foreach (var row in _bonusRows)
        {
            string sid = row.Stat.Text?.Trim();
            if (string.IsNullOrEmpty(sid)) continue;
            if (!seenB.Add(sid))
                warnings.Add($"statBonuses: дубликат '{sid}'");
            bonuses[sid] = (int)row.Value.Value;
        }
        r.StatBonuses = bonuses;

        if (r.Weight == null) r.Weight = new RaceWeightInfo();
        r.Weight.Population = (int)_populationSpin.Value;

        var classes = new Dictionary<string, int>();
        var seenC = new HashSet<string>();
        foreach (var row in _classWeightRows)
        {
            string cid = row.ClassId.Text?.Trim();
            if (string.IsNullOrEmpty(cid)) continue;
            if (!seenC.Add(cid))
                warnings.Add($"weight.classes: дубликат '{cid}'");
            classes[cid] = (int)row.Weight.Value;
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