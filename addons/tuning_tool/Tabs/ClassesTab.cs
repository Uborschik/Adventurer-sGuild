#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

[Tool]
public partial class ClassesTab : Control
{
    private const string ClassesPath = "res://Resources/Data/Adventurer/AdventurerClasses.json";

    private ItemList _list;
    private VBoxContainer _editorRoot;
    private VBoxContainer _growthBox;
    private Label _status;

    private LineEdit _idEdit, _nameRuEdit, _nameEnEdit;
    private LineEdit _primaryStatEdit, _primarySkillEdit;
    private LineEdit _secondaryStatEdit, _secondarySkillEdit;
    private SpinBox _weightSpin;

    private readonly List<AdventurerClassInfo> _classes = new();
    private AdventurerClassInfo _current;

    private sealed class GrowthRow
    {
        public LineEdit Stat;
        public SpinBox Weight;
    }
    private readonly List<GrowthRow> _growthRows = new();

    private enum StatusKind { Ok, Warning, Error }

    public override void _Ready()
    {
        GD.Print("[ClassesTab] _Ready вызван");
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

    // ==================== Левая колонка ====================

    private void BuildLeft(Control parent)
    {
        var left = new VBoxContainer { CustomMinimumSize = new Vector2(200, 0) };
        parent.AddChild(left);
        left.AddChild(new Label { Text = "Классы" });

        _list = new ItemList
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 300),
        };
        _list.ItemSelected += OnClassSelected;
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
        scroll.AddChild(_editorRoot);

        _idEdit = AddTextRow(_editorRoot, "Id", "Уникальный идентификатор класса. Read-only.");
        _idEdit.Editable = false;

        _nameRuEdit = AddTextRow(_editorRoot, "Name RU", "Русское название класса.");
        _nameEnEdit = AddTextRow(_editorRoot, "Name EN", "Английское название класса.");

        _editorRoot.AddChild(new HSeparator());

        _primaryStatEdit = AddTextRow(_editorRoot, "Primary stat",
            "Основной стат. Должен существовать в AdventurerStats.json.\nНапример: str, dex, end, int, wis, cha.");
        _primarySkillEdit = AddTextRow(_editorRoot, "Primary skill",
            "Основной навык. Должен принадлежать primary stat.\nСмотри AdventurerStats.json → stats[].skills[].id.");
        _secondaryStatEdit = AddTextRow(_editorRoot, "Secondary stat",
            "Дополнительный стат. Не должен совпадать с primary.");
        _secondarySkillEdit = AddTextRow(_editorRoot, "Secondary skill",
            "Дополнительный навык. Должен принадлежать secondary stat.");

        _editorRoot.AddChild(new HSeparator());
        _editorRoot.AddChild(new Label { Text = "Growth weights" });
        _editorRoot.AddChild(new Label
        {
            Text = "Веса роста статов. Primary stat обычно = 1.00, остальные меньше.",
            Modulate = new Color(0.7f, 0.7f, 0.7f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        _growthBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _editorRoot.AddChild(_growthBox);

        var addStatBtn = new Button { Text = "+ stat" };
        addStatBtn.Pressed += () => AddGrowthRow(null, null);
        _editorRoot.AddChild(addStatBtn);

        _weightSpin = SpinRow(_editorRoot, "Weight",
            0, 1000, 1, 0,
            "Вес класса при генерации авантюристов. Чем больше — тем чаще попадается.");
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

    // ==================== Список ====================

    private void RefreshList()
    {
        _list.Clear();
        foreach (var c in _classes)
            _list.AddItem(c.Id);
    }

    private void SelectFirst()
    {
        if (_classes.Count == 0) return;
        _list.Select(0);
        OnClassSelected(0);
    }

    private void OnClassSelected(long index)
    {
        if (index < 0 || index >= _classes.Count) return;
        _current = _classes[(int)index];
        LoadCurrentIntoUI();
    }

    private void LoadCurrentIntoUI()
    {
        var c = _current;

        _idEdit.Text = c.Id ?? "";
        _nameRuEdit.Text = c.Name.Ru ?? "";
        _nameEnEdit.Text = c.Name.En ?? "";
        _primaryStatEdit.Text = c.PrimaryStat ?? "";
        _primarySkillEdit.Text = c.PrimarySkill ?? "";
        _secondaryStatEdit.Text = c.SecondaryStat ?? "";
        _secondarySkillEdit.Text = c.SecondarySkill ?? "";
        _weightSpin.Value = c.Weight;

        foreach (var child in _growthBox.GetChildren()) child.QueueFree();
        _growthRows.Clear();

        if (c.GrowthWeights != null)
            foreach (var kv in c.GrowthWeights)
                AddGrowthRow(kv.Key, kv.Value);
    }

    private void AddGrowthRow(string statId, double? weight)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var statEdit = new LineEdit
        {
            Text = statId ?? "",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            PlaceholderText = "stat id",
        };
        var weightSpin = new SpinBox
        {
            MinValue = 0,
            MaxValue = 10,
            Step = 0.01,
            Value = weight ?? 0,
            CustomMinimumSize = new Vector2(100, 0),
        };
        var rm = new Button { Text = "X" };
        var gr = new GrowthRow { Stat = statEdit, Weight = weightSpin };
        rm.Pressed += () => { _growthRows.Remove(gr); row.QueueFree(); };

        row.AddChild(statEdit);
        row.AddChild(weightSpin);
        row.AddChild(rm);
        _growthBox.AddChild(row);
        _growthRows.Add(gr);
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
                var dict = new Dictionary<string, AdventurerClassInfo>();
                foreach (var c in _classes) dict[c.Id] = c;
                AdventurerDatabase.Install(classes: dict);

                string suffix = warnings.Count > 0
                    ? $" Предупреждения: {string.Join("; ", warnings)}"
                    : "";
                SetStatus($"Preview: {_classes.Count} классов в памяти (диск не тронут).{suffix}",
                          StatusKind.Warning);
            }
            catch (Exception e)
            {
                SetStatus($"Preview FAILED: {e.Message}", StatusKind.Error);
                GD.PushError($"[ClassesTab] Preview: {e}");
            }
            return;
        }

        // ─── Обычный путь — без изменений ───
        // ... ваш текущий код Apply ...
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
            SetStatus($"Reverted: {_classes.Count} классов перезагружено", StatusKind.Ok);
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

        c.Name = new LocalizedString { Ru = _nameRuEdit.Text, En = _nameEnEdit.Text };
        c.PrimaryStat = EmptyToNull(_primaryStatEdit.Text);
        c.PrimarySkill = EmptyToNull(_primarySkillEdit.Text);
        c.SecondaryStat = EmptyToNull(_secondaryStatEdit.Text);
        c.SecondarySkill = EmptyToNull(_secondarySkillEdit.Text);
        c.Weight = (int)_weightSpin.Value;

        var weights = new Dictionary<string, double>();
        var seen = new HashSet<string>();
        foreach (var row in _growthRows)
        {
            string sid = row.Stat.Text?.Trim();
            if (string.IsNullOrEmpty(sid)) continue;
            if (!seen.Add(sid))
                warnings.Add($"growthWeights: дубликат '{sid}'");
            weights[sid] = NumericHelpers.Round4(row.Weight.Value);
        }
        c.GrowthWeights = weights;

        return warnings;
    }

    private static string EmptyToNull(string s) => string.IsNullOrEmpty(s) ? null : s;

    private void ReloadFromDisk()
    {
        try
        {
            var db = JsonLoader.Load<AdventurerClassDatabase>(ClassesPath);
            _classes.Clear();
            if (db?.Classes != null) _classes.AddRange(db.Classes);
            GD.Print($"[ClassesTab] ReloadFromDisk: загружено {_classes.Count} классов");
        }
        catch (Exception e)
        {
            GD.PushError($"[ClassesTab] Не удалось загрузить {ClassesPath}: {e}");
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