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
using AdventurersGuild.Domain.Quests;
using Godot;

[Tool]
public partial class PhasesTab : Control
{
    private const string PhasesPath = "res://Resources/Data/Quest/QuestPhases.json";

    private const float LabelW = 130;
    private const float FormWidth = 750;

    private ItemList _list;
    private VBoxContainer _editorRoot;
    private VBoxContainer _solutionsBox;
    private Label _status;

    private LineEdit _idEdit;
    private LineEdit _nameRuEdit, _nameEnEdit;
    private SpinBox _durationSpin;
    private CheckBox _criticalCheck;
    private SpinBox _expSpin;

    private CheckBox _hasOnFailCheck;
    private HBoxContainer _onFailRow;
    private OptionButton _onFailTargetOption;
    private SpinBox _onFailDcDeltaSpin, _onFailDurationSpin;

    private readonly List<QuestPhaseTemplate> _phases = new();
    private QuestPhaseTemplate _current;
    private bool _suppressSignals;

    private enum StatusKind { Ok, Warning, Error }

    private sealed class SkillRow
    {
        public OptionButton SkillOption;
        public SpinBox Dc;
    }

    private sealed class SolutionRow
    {
        public VBoxContainer Root;
        public List<SkillRow> Skills = new();
    }

    private readonly List<SolutionRow> _solutionRows = new();
    private readonly List<string> _phaseIds = new();

    public override void _Ready()
    {
        GD.Print("[PhasesTab] _Ready вызван");
        ReloadFromDisk();
        RebuildPhaseIds();

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

    // ==================== Левая колонка ====================

    private void BuildLeft(Control parent)
    {
        var left = new VBoxContainer { CustomMinimumSize = new Vector2(200, 0) };
        left.AddThemeConstantOverride("separation", 4);
        parent.AddChild(left);
        left.AddChild(new Label { Text = "Фазы" });

        _list = new ItemList
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 300),
        };
        _list.ItemSelected += OnPhaseSelected;
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

        var (_, infoBox) = CollapsibleSection(_editorRoot, "Info", expanded: true);
        BuildInfoSection(infoBox);

        var (_, onFailBox) = CollapsibleSection(_editorRoot, "OnFail", expanded: true);
        BuildOnFailSection(onFailBox);

        var (_, solBox) = CollapsibleSection(_editorRoot, "Solutions", expanded: true);
        BuildSolutionsSection(solBox);
    }

    private void BuildInfoSection(Control parent)
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

        var durRow = FormRow(parent);
        durRow.AddChild(FormLabel("Duration (min)"));
        _durationSpin = SpinBox(0, 100000, 1, 0);
        durRow.AddChild(_durationSpin);

        durRow.AddChild(FormLabel("Exp reward"));
        _expSpin = SpinBox(0, 10000, 1, 0);
        durRow.AddChild(_expSpin);

        var critRow = FormRow(parent);
        critRow.AddChild(FormLabel("Critical"));
        _criticalCheck = new CheckBox();
        critRow.AddChild(_criticalCheck);
        critRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
    }

    private void BuildOnFailSection(Control parent)
    {
        var toggleRow = FormRow(parent);
        toggleRow.AddChild(FormLabel("Has OnFail"));
        _hasOnFailCheck = new CheckBox();
        _hasOnFailCheck.Toggled += OnHasOnFailToggled;
        toggleRow.AddChild(_hasOnFailCheck);
        toggleRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        _onFailRow = new HBoxContainer { Visible = false };
        _onFailRow.AddThemeConstantOverride("separation", 8);
        parent.AddChild(_onFailRow);

        var wrap = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _onFailRow.AddChild(wrap);

        var row = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(FormWidth, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        row.AddThemeConstantOverride("separation", 8);
        wrap.AddChild(row);

        row.AddChild(FormLabel("Target phase"));
        _onFailTargetOption = new OptionButton
        {
            CustomMinimumSize = new Vector2(200, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        row.AddChild(_onFailTargetOption);

        row.AddChild(FormLabel("dcΔ"));
        _onFailDcDeltaSpin = SpinBox(-10, 10, 0.05, 0);
        _onFailDcDeltaSpin.CustomMinimumSize = new Vector2(100, 0);
        _onFailDcDeltaSpin.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        row.AddChild(_onFailDcDeltaSpin);

        row.AddChild(FormLabel("durΔ"));
        _onFailDurationSpin = SpinBox(-10000, 10000, 1, 0);
        _onFailDurationSpin.CustomMinimumSize = new Vector2(100, 0);
        _onFailDurationSpin.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        row.AddChild(_onFailDurationSpin);
    }

    private void BuildSolutionsSection(Control parent)
    {
        parent.AddChild(new Label
        {
            Text = "Solutions. Фаза проходит, если хотя бы одно решение прошло все свои скиллы.",
            Modulate = new Color(0.7f, 0.7f, 0.7f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        _solutionsBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _solutionsBox.AddThemeConstantOverride("separation", 8);
        parent.AddChild(_solutionsBox);

        var addSolBtn = new Button
        {
            Text = "+ solution",
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        addSolBtn.Pressed += OnAddSolutionPressed;
        parent.AddChild(addSolBtn);
    }

    // ==================== Solutions ====================

    private void AddSolutionUI(PhaseSolution sol)
    {
        var solRow = new SolutionRow();

        var frame = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        root.AddThemeConstantOverride("separation", 6);
        frame.AddChild(root);

        var header = new HBoxContainer();
        header.AddChild(new Label
        {
            Text = $"Solution {_solutionRows.Count}",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        });
        var rm = new Button
        {
            Text = "×",
            Flat = true,
            CustomMinimumSize = new Vector2(28, 0),
        };
        rm.Pressed += () =>
        {
            _solutionRows.Remove(solRow);
            frame.QueueFree();
        };
        header.AddChild(rm);
        root.AddChild(header);

        var skillsBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        skillsBox.AddThemeConstantOverride("separation", 4);
        root.AddChild(skillsBox);

        var addSkillBtn = new Button
        {
            Text = "+ skill",
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        addSkillBtn.Pressed += () => AddSkillUI(solRow, skillsBox, null, 0.0);
        root.AddChild(addSkillBtn);

        _solutionsBox.AddChild(frame);
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
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        var skillOption = new OptionButton
        {
            CustomMinimumSize = new Vector2(220, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        PopulateSkillOptions(skillOption, skillId);
        row.AddChild(skillOption);

        var dcSpin = new SpinBox
        {
            MinValue = 0,
            MaxValue = 10,
            Step = 0.01,
            Value = dc,
            CustomMinimumSize = new Vector2(100, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        row.AddChild(dcSpin);

        var rm = new Button
        {
            Text = "×",
            Flat = true,
            CustomMinimumSize = new Vector2(28, 0),
        };
        var skillRow = new SkillRow { SkillOption = skillOption, Dc = dcSpin };
        rm.Pressed += () =>
        {
            solRow.Skills.Remove(skillRow);
            row.QueueFree();
        };
        row.AddChild(rm);

        parent.AddChild(row);
        solRow.Skills.Add(skillRow);
    }

    private static void PopulateSkillOptions(OptionButton option, string currentId)
    {
        option.Clear();
        foreach (var sid in SkillIds.All)
        {
            string name = AdventurerDatabase.GetSkill(sid)?.Name.Get(Loc.Language) ?? "";
            option.AddItem(string.IsNullOrEmpty(name) ? sid : $"{sid}  ({name})");
        }

        if (!string.IsNullOrEmpty(currentId) && !SkillIds.All.Contains(currentId))
        {
            option.AddItem($"{currentId}  (неизвестный)");
            option.Selected = option.ItemCount - 1;
            return;
        }

        for (int i = 0; i < option.ItemCount; i++)
        {
            if (ReadOptionId(option, i) == currentId) { option.Selected = i; return; }
        }
        if (option.ItemCount > 0) option.Selected = 0;
    }

    private void PopulateOnFailTargetOptions(string currentTarget)
    {
        _onFailTargetOption.Clear();
        _onFailTargetOption.AddItem("(none)");
        foreach (var pid in _phaseIds)
            _onFailTargetOption.AddItem(pid);

        if (string.IsNullOrEmpty(currentTarget)) { _onFailTargetOption.Selected = 0; return; }

        for (int i = 0; i < _onFailTargetOption.ItemCount; i++)
        {
            if (_onFailTargetOption.GetItemText(i) == currentTarget)
            {
                _onFailTargetOption.Selected = i;
                return;
            }
        }
        _onFailTargetOption.Selected = 0;
    }

    private static string ReadOptionId(OptionButton option, int index)
    {
        string text = option.GetItemText(index);
        int spaceIdx = text.IndexOf("  (", StringComparison.Ordinal);
        return spaceIdx > 0 ? text.Substring(0, spaceIdx) : text;
    }

    private static string ReadSelectedId(OptionButton option)
    {
        if (option.Selected < 0) return null;
        string id = ReadOptionId(option, option.Selected);
        return id == "(none)" ? null : id;
    }

    private void OnAddSolutionPressed()
    {
        AddSolutionUI(new PhaseSolution { Skills = new Dictionary<string, double>() });
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

    private static SpinBox SpinBox(double min, double max, double step, double value)
    {
        return new SpinBox
        {
            MinValue = min,
            MaxValue = max,
            Step = step,
            Value = value,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
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
        PopulateOnFailTargetOptions(p.OnFail?.TargetPhase);
        _onFailDcDeltaSpin.Value = p.OnFail?.DcDelta ?? 0;
        _onFailDurationSpin.Value = p.OnFail?.DurationDeltaMinutes ?? 0;
        _onFailRow.Visible = hasOnFail;

        foreach (var child in _solutionsBox.GetChildren())
            child.QueueFree();
        _solutionRows.Clear();
        if (p.Solutions != null)
            foreach (var sol in p.Solutions)
                AddSolutionUI(sol);

        var warnings = new List<string>();
        if (string.IsNullOrEmpty(p.Id)) warnings.Add("пустой phase id");
        if (p.Solutions == null || p.Solutions.Count == 0)
        {
            if (p.Id != "short_rest" && p.Id != "long_rest")
                warnings.Add("нет solutions — фаза не пройдёт");
        }

        SetStatus(warnings.Count > 0 ? string.Join("; ", warnings) : "",
                  warnings.Count > 0 ? StatusKind.Warning : StatusKind.Ok);

        _suppressSignals = false;
    }

    private void OnHasOnFailToggled(bool pressed)
    {
        _onFailRow.Visible = pressed;
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
                var dict = new Dictionary<string, QuestPhaseTemplate>();
                foreach (var p in _phases) dict[p.Id] = p;
                QuestDatabase.ApplyData(phases: dict);

                SetStatus($"Preview: {_phases.Count} фаз в памяти (диск не тронут).",
                          StatusKind.Warning);
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
            SetStatus($"Revert: Load упал: {e.Message}", StatusKind.Error);
        }
    }

    private List<string> SyncUIToCurrent()
    {
        var warnings = new List<string>();
        var p = _current;

        p.Name = new LocalizedString { Ru = _nameRuEdit.Text, En = _nameEnEdit.Text };
        p.BaseDurationMinutes = (int)_durationSpin.Value;
        p.Critical = _criticalCheck.ButtonPressed;
        p.ExpReward = (int)_expSpin.Value;

        if (_hasOnFailCheck.ButtonPressed)
        {
            p.OnFail = new PhaseFailEffect
            {
                TargetPhase = ReadSelectedId(_onFailTargetOption),
                DcDelta = NumericHelpers.Round4(_onFailDcDeltaSpin.Value),
                DurationDeltaMinutes = (int)_onFailDurationSpin.Value,
            };
        }
        else
        {
            p.OnFail = null;
        }

        var sols = new List<PhaseSolution>();
        var seenGlobal = new HashSet<string>();
        for (int si = 0; si < _solutionRows.Count; si++)
        {
            var solRow = _solutionRows[si];
            var skills = new Dictionary<string, double>();
            foreach (var s in solRow.Skills)
            {
                string sid = ReadSelectedId(s.SkillOption);
                if (string.IsNullOrEmpty(sid)) continue;
                if (!skills.ContainsKey(sid))
                    skills[sid] = NumericHelpers.Round4(s.Dc.Value);
            }

            if (skills.Count == 0)
                warnings.Add($"solution {si} пустой");
            else
            {
                sols.Add(new PhaseSolution { Skills = skills });
            }
        }
        p.Solutions = sols;

        return warnings;
    }

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