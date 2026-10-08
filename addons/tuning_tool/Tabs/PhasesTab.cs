#if TOOLS
using System;
using System.Collections.Generic;
using Godot;

[Tool]
public partial class PhasesTab : Control
{
    private const string PhasesPath = "res://Resources/Data/Quest/QuestPhases.json";

    private ItemList _list;
    private VBoxContainer _editorRoot;
    private VBoxContainer _solutionsBox;
    private Label _status;

    private LineEdit _idEdit;
    private LineEdit _nameRuEdit;
    private LineEdit _nameEnEdit;
    private SpinBox _durationSpin;
    private CheckBox _criticalCheck;
    private SpinBox _expSpin;

    private CheckBox _hasOnFailCheck;
    private HBoxContainer _onFailRow;
    private LineEdit _onFailTargetEdit;
    private SpinBox _onFailDcDeltaSpin;
    private SpinBox _onFailDurationSpin;

    private readonly List<QuestPhaseTemplate> _phases = new();
    private QuestPhaseTemplate _current;
    private bool _suppressSignals;

    private sealed class SkillRow
    {
        public LineEdit Skill;
        public SpinBox Dc;
        public HBoxContainer Root;
    }

    private sealed class SolutionRow
    {
        public VBoxContainer Root;
        public List<SkillRow> Skills = new();
    }

    private readonly List<SolutionRow> _solutionRows = new();
    private enum StatusKind { Ok, Warning, Error }

    public override void _Ready()
    {
        GD.Print("[PhasesTab] _Ready вызван");
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

        left.AddChild(new Label { Text = "Фазы" });

        _list = new ItemList
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 300),
        };

        _list.ItemSelected += OnPhaseSelected;
        left.AddChild(_list);

        var btnRow = new HBoxContainer();
        btnRow.SizeFlagsVertical = SizeFlags.ShrinkEnd;

        var applyBtn = new Button { Text = "Apply", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        applyBtn.Pressed += OnApplyPressed;

        var revertBtn = new Button { Text = "Revert", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        revertBtn.Pressed += OnRevertPressed;

        btnRow.AddChild(applyBtn);
        btnRow.AddChild(revertBtn);
        left.AddChild(btnRow);

        _status = new Label
        {
            Text = "",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsVertical = SizeFlags.ShrinkEnd
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

        // Id
        _idEdit = AddTextRow(_editorRoot, "Id");
        _idEdit.Editable = false;

        // Name Ru / En
        _nameRuEdit = AddTextRow(_editorRoot, "Name RU");
        _nameEnEdit = AddTextRow(_editorRoot, "Name EN");

        // Duration / Critical / Exp
        _durationSpin = AddSpinRow(_editorRoot, "Duration (min)", 0, 100000, 1, 1);
        _criticalCheck = AddCheckRow(_editorRoot, "Critical");
        _expSpin = AddSpinRow(_editorRoot, "Exp reward", 0, 10000, 1, 1);

        // OnFail
        _editorRoot.AddChild(new HSeparator());
        _hasOnFailCheck = AddCheckRow(_editorRoot, "Has OnFail");
        _hasOnFailCheck.Toggled += OnHasOnFailToggled;

        _onFailRow = new HBoxContainer();
        _onFailTargetEdit = new LineEdit { SizeFlagsHorizontal = SizeFlags.ExpandFill, PlaceholderText = "targetPhase (опц.)" };
        _onFailDcDeltaSpin = new SpinBox { MinValue = -10, MaxValue = 10, Step = 0.05, Value = 0 };
        _onFailDurationSpin = new SpinBox { MinValue = -10000, MaxValue = 10000, Step = 1, Value = 0 };
        _onFailRow.AddChild(new Label { Text = "target" });
        _onFailRow.AddChild(_onFailTargetEdit);
        _onFailRow.AddChild(new Label { Text = "dcΔ" });
        _onFailRow.AddChild(_onFailDcDeltaSpin);
        _onFailRow.AddChild(new Label { Text = "durΔ" });
        _onFailRow.AddChild(_onFailDurationSpin);
        _editorRoot.AddChild(_onFailRow);

        // Solutions
        _editorRoot.AddChild(new HSeparator());
        _editorRoot.AddChild(new Label { Text = "Solutions" });

        _solutionsBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _editorRoot.AddChild(_solutionsBox);

        var addSolBtn = new Button { Text = "+ Add solution" };
        addSolBtn.Pressed += OnAddSolutionPressed;
        _editorRoot.AddChild(addSolBtn);
    }

    private static LineEdit AddTextRow(Control parent, string label)
    {
        var row = new HBoxContainer();
        row.AddChild(new Label { Text = label, CustomMinimumSize = new Vector2(120, 0) });
        var edit = new LineEdit { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(edit);
        parent.AddChild(row);
        return edit;
    }

    private static SpinBox AddSpinRow(Control parent, string label, double min, double max, double step, double val)
    {
        var row = new HBoxContainer();
        row.AddChild(new Label { Text = label, CustomMinimumSize = new Vector2(120, 0) });
        var spin = new SpinBox { MinValue = min, MaxValue = max, Step = step, Value = val, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(spin);
        parent.AddChild(row);
        return spin;
    }

    private static CheckBox AddCheckRow(Control parent, string label)
    {
        var row = new HBoxContainer();
        row.AddChild(new Label { Text = label, CustomMinimumSize = new Vector2(120, 0) });
        var chk = new CheckBox();
        row.AddChild(chk);
        parent.AddChild(row);
        return chk;
    }

    // ==================== Список ====================

    private void RefreshList()
    {
        _suppressSignals = true;
        _list.Clear();
        for (int i = 0; i < _phases.Count; i++)
            _list.AddItem(_phases[i].Id);
        _suppressSignals = false;
    }

    private void SelectFirst()
    {
        if (_phases.Count == 0) return;
        _list.Select(0);
        OnPhaseSelected(0);
    }

    private void OnPhaseSelected(long index)
    {
        if (_suppressSignals) return;
        if (index < 0 || index >= _phases.Count) return;

        _current = _phases[(int)index];
        LoadCurrentIntoUI();
    }

    private void LoadCurrentIntoUI()
    {
        _suppressSignals = true;

        var p = _current;
        _idEdit.Text = p.Id ?? "";
        _nameRuEdit.Text = p.Name.Ru ?? "";
        _nameEnEdit.Text = p.Name.En ?? "";
        _durationSpin.Value = p.BaseDurationMinutes;
        _criticalCheck.ButtonPressed = p.Critical;
        _expSpin.Value = p.ExpReward;

        bool hasOnFail = p.OnFail != null;
        _hasOnFailCheck.ButtonPressed = hasOnFail;
        _onFailTargetEdit.Text = p.OnFail?.TargetPhase ?? "";
        _onFailDcDeltaSpin.Value = p.OnFail?.DcDelta ?? 0;
        _onFailDurationSpin.Value = p.OnFail?.DurationDeltaMinutes ?? 0;
        SetOnFailRowVisible(hasOnFail);

        RebuildSolutionsUI(p.Solutions);

        _suppressSignals = false;
    }

    private void SetOnFailRowVisible(bool v)
    {
        _onFailRow.Visible = v;
    }

    private void OnHasOnFailToggled(bool pressed)
    {
        SetOnFailRowVisible(pressed);
    }

    // ==================== Solutions UI ====================

    private void RebuildSolutionsUI(List<PhaseSolution> solutions)
    {
        foreach (var child in _solutionsBox.GetChildren())
            child.QueueFree();
        _solutionRows.Clear();

        if (solutions == null) return;

        for (int si = 0; si < solutions.Count; si++)
            AddSolutionUI(solutions[si]);
    }

    private void AddSolutionUI(PhaseSolution sol)
    {
        var solRow = new SolutionRow();

        var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var header = new HBoxContainer();
        header.AddChild(new Label { Text = $"Solution {_solutionRows.Count}", SizeFlagsHorizontal = SizeFlags.ExpandFill });
        var removeBtn = new Button { Text = "X" };
        removeBtn.Pressed += () =>
        {
            _solutionRows.Remove(solRow);
            solRow.Root.QueueFree();
        };
        header.AddChild(removeBtn);
        root.AddChild(header);

        var skillsBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        root.AddChild(skillsBox);

        var addSkillBtn = new Button { Text = "+ skill" };
        addSkillBtn.Pressed += () => AddSkillUI(solRow, skillsBox, null, 0.0);
        root.AddChild(addSkillBtn);

        _solutionsBox.AddChild(root);
        solRow.Root = root;
        _solutionRows.Add(solRow);

        if (sol?.Skills != null)
        {
            foreach (var kv in sol.Skills)
                AddSkillUI(solRow, skillsBox, kv.Key, kv.Value);
        }
    }

    private void AddSkillUI(SolutionRow solRow, VBoxContainer parent, string skillId, double dc)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

        var skillEdit = new LineEdit
        {
            Text = skillId ?? "",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            PlaceholderText = "skill id",
        };
        var dcSpin = new SpinBox
        {
            MinValue = 0,
            MaxValue = 10,
            Step = 0.01,
            Value = dc,
            CustomMinimumSize = new Vector2(90, 0),
        };
        var rm = new Button { Text = "X" };

        var skillRow = new SkillRow { Skill = skillEdit, Dc = dcSpin, Root = row };

        rm.Pressed += () =>
        {
            solRow.Skills.Remove(skillRow);
            row.QueueFree();
        };

        row.AddChild(skillEdit);
        row.AddChild(dcSpin);
        row.AddChild(rm);
        parent.AddChild(row);

        solRow.Skills.Add(skillRow);
    }

    private void OnAddSolutionPressed()
    {
        AddSolutionUI(new PhaseSolution { Skills = new Dictionary<string, double>() });
    }

    // ==================== Apply / Revert ====================

    private void OnApplyPressed()
    {
        if (_current == null) { SetStatus("Ничего не выбрано", StatusKind.Error); return; }

        SyncUIToCurrent();

        if (TuningDock.PreviewEnabled)
        {
            try
            {
                var dict = new System.Collections.Generic.Dictionary<string, QuestPhaseTemplate>();
                foreach (var p in _phases) dict[p.Id] = p;
                QuestDatabase.ApplyData(phases: dict);
                SetStatus($"Preview: {_phases.Count} фаз в памяти (диск не тронут)", StatusKind.Warning);
            }
            catch (Exception e)
            {
                SetStatus($"Preview FAILED: {e.Message}", StatusKind.Error);
                GD.PushError($"[PhasesTab] Preview: {e}");
            }
            return;
        }

        try
        {
            var db = new QuestPhaseDatabase { Phases = _phases };
            JsonWriter.Write(PhasesPath, db);
        }
        catch (Exception e)
        {
            SetStatus($"Apply FAILED: {e.Message}", StatusKind.Error);
            GD.PushError($"[PhasesTab] Apply: {e}");
            return;
        }

        try
        {
            QuestDatabase.Load();
            SetStatus($"Applied: {_phases.Count} фаз записано в JSON", StatusKind.Ok);
        }
        catch (Exception e)
        {
            SetStatus($"Apply ok, но QuestDatabase.Load упал: {e.Message}", StatusKind.Error);
            GD.PushError($"[PhasesTab] QuestDatabase.Load: {e}");
        }
    }

    private void OnRevertPressed()
    {
        ReloadFromDisk();
        RefreshList();
        SelectFirst();
        try
        {
            QuestDatabase.Load();
            SetStatus($"Reverted: {_phases.Count} фаз перезагружено", StatusKind.Ok);
        }
        catch (Exception e)
        {
            SetStatus($"Revert: QuestDatabase.Load упал: {e.Message}", StatusKind.Error);
        }
    }

    // ==================== Sync UI -> модель ====================

    private void SyncUIToCurrent()
    {
        var p = _current;

        p.Name = new LocalizedString { Ru = _nameRuEdit.Text, En = _nameEnEdit.Text };
        p.BaseDurationMinutes = (int)_durationSpin.Value;
        p.Critical = _criticalCheck.ButtonPressed;
        p.ExpReward = (int)_expSpin.Value;

        if (_hasOnFailCheck.ButtonPressed)
        {
            p.OnFail = new PhaseFailEffect
            {
                TargetPhase = string.IsNullOrWhiteSpace(_onFailTargetEdit.Text) ? null : _onFailTargetEdit.Text,
                DcDelta = _onFailDcDeltaSpin.Value,
                DurationDeltaMinutes = (int)_onFailDurationSpin.Value,
            };
        }
        else
        {
            p.OnFail = null;
        }

        var sols = new List<PhaseSolution>();
        foreach (var solRow in _solutionRows)
        {
            var skills = new Dictionary<string, double>();
            foreach (var s in solRow.Skills)
            {
                string sid = s.Skill.Text?.Trim();
                if (string.IsNullOrEmpty(sid)) continue;
                skills[sid] = NumericHelpers.Round4(s.Dc.Value);
            }
            sols.Add(new PhaseSolution { Skills = skills });
        }
        p.Solutions = sols;
    }

    // ==================== Прочее ====================

    private void ReloadFromDisk()
    {
        try
        {
            var db = JsonLoader.Load<QuestPhaseDatabase>(PhasesPath);
            _phases.Clear();
            if (db?.Phases != null) _phases.AddRange(db.Phases);
            GD.Print($"[PhasesTab] ReloadFromDisk: загружено {_phases.Count} фаз");
        }
        catch (Exception e)
        {
            GD.PushError($"[PhasesTab] Не удалось загрузить {PhasesPath}: {e}");
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