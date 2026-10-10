#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Core;
using AdventurersGuild.Data.Balance;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Domain.Quests;
using Godot;

[Tool]
public partial class BalanceTab : Control
{
    private const string QuestBalancePath = "res://Resources/Data/Quest/QuestBalance.json";

    private const float LabelW = 180;
    private const float FormWidth = 750;

    private VBoxContainer _editorRoot;
    private Label _status;

    private LineEdit _idEdit;
    private SpinBox _durMultiplierSpin, _durFailPenaltySpin, _durIntBonusCapSpin, _durIntBonusKSpin;
    private SpinBox _skillBaseSpin, _skillPerRatioSpin, _skillMinSpin, _skillMaxSpin;
    private SpinBox _escapeStatKSpin, _escapeStatCapSpin, _escapeBaseSpin;
    private SpinBox _escapeEndWeightSpin, _escapeWisWeightSpin;

    private SpinBox _wfMinSpin, _wfMaxSpin;
    private SpinBox _wsChanceSpin, _wsMinSpin, _wsMaxSpin;

    private SpinBox _nightAttackChanceSpin, _maxQuestDaysSpin, _nightCreatureLvlPenaltySpin;
    private SpinBox _endInjuryKSpin, _endInjuryCapSpin;
    private SpinBox _endDeathKSpin, _endDeathCapSpin;
    private SpinBox _expContributionBaseScoreSpin;

    private QuestBalanceDatabase _db;
    private QuestBalanceProfile _quest;

    private enum StatusKind { Ok, Warning, Error }

    public override void _Ready()
    {
        GD.Print("[BalanceTab] _Ready вызван");

        if (!ReloadFromDisk())
        {
            var err = new Label { Text = "Не удалось загрузить QuestBalance.json (см. Output)" };
            AddChild(err);
            return;
        }

        var root = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(root);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        root.AddChild(scroll);

        _editorRoot = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _editorRoot.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(_editorRoot);

        var (_, idBox) = CollapsibleSection(_editorRoot, "Identity", expanded: true);
        var (_, durBox) = CollapsibleSection(_editorRoot, "Duration", expanded: true);
        var (_, skillBox) = CollapsibleSection(_editorRoot, "Skill Roll", expanded: true);
        var (_, escapeBox) = CollapsibleSection(_editorRoot, "Escape", expanded: true);
        var (_, woundsBox) = CollapsibleSection(_editorRoot, "Wounds", expanded: false);
        var (_, nightBox) = CollapsibleSection(_editorRoot, "Night & Limits", expanded: false);
        var (_, endurBox) = CollapsibleSection(_editorRoot, "Endurance", expanded: false);
        var (_, expBox) = CollapsibleSection(_editorRoot, "Experience", expanded: false);

        BuildIdentity(idBox);
        BuildDuration(durBox);
        BuildSkillRoll(skillBox);
        BuildEscape(escapeBox);
        BuildWounds(woundsBox);
        BuildNightLimits(nightBox);
        BuildEndurance(endurBox);
        BuildExperience(expBox);

        var btnRow = new HBoxContainer();
        var applyBtn = new Button { Text = "Apply", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var revertBtn = new Button { Text = "Revert", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        applyBtn.Pressed += OnApplyPressed;
        revertBtn.Pressed += OnRevertPressed;
        btnRow.AddChild(applyBtn);
        btnRow.AddChild(revertBtn);
        root.AddChild(btnRow);

        _status = new Label { Text = "" };
        root.AddChild(_status);

        LoadIntoUI();
        SetStatus($"Загружен профиль '{_quest.Id}'", StatusKind.Ok);
    }

    // ==================== Секции ====================

    private void BuildIdentity(Control parent)
    {
        var r = FormRow(parent);
        r.AddChild(FormLabel("Id"));
        _idEdit = FormField();
        _idEdit.Editable = false;
        r.AddChild(_idEdit);

        parent.AddChild(new Label
        {
            Text = "Профиль, который используется игрой. Обычно 'default'.",
            Modulate = new Color(0.7f, 0.7f, 0.7f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });
    }

    private void BuildDuration(Control parent)
    {
        var r1 = FormRow(parent);
        r1.AddChild(FormLabel("Multiplier ×"));
        _durMultiplierSpin = Spin(0, 10, 0.05, 0);
        r1.AddChild(_durMultiplierSpin);
        r1.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        var r2 = FormRow(parent);
        r2.AddChild(FormLabel("Fail penalty/phase"));
        _durFailPenaltySpin = Spin(0, 1, 0.01, 0);
        r2.AddChild(_durFailPenaltySpin);

        r2.AddChild(FormLabel("Int bonus cap"));
        _durIntBonusCapSpin = Spin(0, 5, 0.01, 0);
        r2.AddChild(_durIntBonusCapSpin);

        var r3 = FormRow(parent);
        r3.AddChild(FormLabel("Int bonus K"));
        _durIntBonusKSpin = Spin(0, 5, 0.01, 0);
        r3.AddChild(_durIntBonusKSpin);
        r3.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
    }

    private void BuildSkillRoll(Control parent)
    {
        var r = FormRow(parent);
        r.AddChild(FormLabel("base"));
        _skillBaseSpin = Spin(0, 200, 1, 0);
        r.AddChild(_skillBaseSpin);
        r.AddChild(FormLabel("perRatio"));
        _skillPerRatioSpin = Spin(0, 200, 1, 0);
        r.AddChild(_skillPerRatioSpin);

        var r2 = FormRow(parent);
        r2.AddChild(FormLabel("min"));
        _skillMinSpin = Spin(0, 100, 1, 0);
        r2.AddChild(_skillMinSpin);
        r2.AddChild(FormLabel("max"));
        _skillMaxSpin = Spin(0, 100, 1, 0);
        r2.AddChild(_skillMaxSpin);

        parent.AddChild(new Label
        {
            Text = "chance = clamp(base + (ratio − 1)·perRatio, min, max)\n" +
                   "ratio  = skillValue / (rawDc × refMax)",
            Modulate = new Color(0.65f, 0.65f, 0.65f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });
    }

    private void BuildEscape(Control parent)
    {
        var r1 = FormRow(parent);
        r1.AddChild(FormLabel("statK"));
        _escapeStatKSpin = Spin(0, 200, 1, 0);
        r1.AddChild(_escapeStatKSpin);
        r1.AddChild(FormLabel("statCap"));
        _escapeStatCapSpin = Spin(0, 100, 1, 0);
        r1.AddChild(_escapeStatCapSpin);

        var r2 = FormRow(parent);
        r2.AddChild(FormLabel("Base"));
        _escapeBaseSpin = Spin(0, 100, 1, 0);
        r2.AddChild(_escapeBaseSpin);
        r2.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        var r3 = FormRow(parent);
        r3.AddChild(FormLabel("endWeight"));
        _escapeEndWeightSpin = Spin(0, 2, 0.01, 0);
        r3.AddChild(_escapeEndWeightSpin);
        r3.AddChild(FormLabel("wisWeight"));
        _escapeWisWeightSpin = Spin(0, 2, 0.01, 0);
        r3.AddChild(_escapeWisWeightSpin);
    }

    private void BuildWounds(Control parent)
    {
        parent.AddChild(new Label
        {
            Text = "Wound on fail — сколько дней ранения при провале escape",
            Modulate = new Color(0.7f, 0.7f, 0.7f),
        });

        var r1 = FormRow(parent);
        r1.AddChild(FormLabel("Days min"));
        _wfMinSpin = Spin(0, 10000, 1, 0, 80);
        r1.AddChild(_wfMinSpin);
        r1.AddChild(FormLabel("max"));
        _wfMaxSpin = Spin(0, 10000, 1, 0, 80);
        r1.AddChild(_wfMaxSpin);

        parent.AddChild(new HSeparator());
        parent.AddChild(new Label
        {
            Text = "Wound on success — шанс и дни ранения при успехе",
            Modulate = new Color(0.7f, 0.7f, 0.7f),
        });

        var r2 = FormRow(parent);
        r2.AddChild(FormLabel("Chance %"));
        _wsChanceSpin = Spin(0, 100, 1, 0, 80);
        r2.AddChild(_wsChanceSpin);
        r2.AddChild(FormLabel("min"));
        _wsMinSpin = Spin(0, 1000, 1, 0, 60);
        r2.AddChild(_wsMinSpin);
        r2.AddChild(FormLabel("max"));
        _wsMaxSpin = Spin(0, 1000, 1, 0, 60);
        r2.AddChild(_wsMaxSpin);
    }

    private void BuildNightLimits(Control parent)
    {
        var r1 = FormRow(parent);
        r1.AddChild(FormLabel("Night attack %"));
        _nightAttackChanceSpin = Spin(0, 100, 1, 0);
        r1.AddChild(_nightAttackChanceSpin);

        r1.AddChild(FormLabel("Max quest days"));
        _maxQuestDaysSpin = Spin(1, 3650, 1, 1);
        r1.AddChild(_maxQuestDaysSpin);

        var r2 = FormRow(parent);
        r2.AddChild(FormLabel("Night level penalty"));
        _nightCreatureLvlPenaltySpin = Spin(0, 100, 1, 0);
        r2.AddChild(_nightCreatureLvlPenaltySpin);
        r2.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
    }

    private void BuildEndurance(Control parent)
    {
        var r1 = FormRow(parent);
        r1.AddChild(FormLabel("Injury K"));
        _endInjuryKSpin = Spin(0, 1000, 1, 0);
        r1.AddChild(_endInjuryKSpin);
        r1.AddChild(FormLabel("Injury cap"));
        _endInjuryCapSpin = Spin(0, 1, 0.01, 0);
        r1.AddChild(_endInjuryCapSpin);

        var r2 = FormRow(parent);
        r2.AddChild(FormLabel("Death K"));
        _endDeathKSpin = Spin(0, 1000, 1, 0);
        r2.AddChild(_endDeathKSpin);
        r2.AddChild(FormLabel("Death cap"));
        _endDeathCapSpin = Spin(0, 1, 0.01, 0);
        r2.AddChild(_endDeathCapSpin);
    }

    private void BuildExperience(Control parent)
    {
        var r = FormRow(parent);
        r.AddChild(FormLabel("Contribution base"));
        _expContributionBaseScoreSpin = Spin(0, 10, 0.01, 0);
        r.AddChild(_expContributionBaseScoreSpin);
        r.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
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

    // ==================== Load / Sync ====================

    private void LoadIntoUI()
    {
        var q = _quest;

        _idEdit.Text = q.Id ?? "";

        _durMultiplierSpin.Value = q.DurationMultiplier;
        _durFailPenaltySpin.Value = q.DurationFailPenaltyPerPhase;
        _durIntBonusCapSpin.Value = q.DurationIntBonusCap;
        _durIntBonusKSpin.Value = q.DurationIntBonusK;

        _skillBaseSpin.Value = q.SkillRoll.Base;
        _skillPerRatioSpin.Value = q.SkillRoll.PerRatio;
        _skillMinSpin.Value = q.SkillRoll.Min;
        _skillMaxSpin.Value = q.SkillRoll.Max;

        _escapeStatKSpin.Value = q.EscapeStatK;
        _escapeStatCapSpin.Value = q.EscapeStatCap;
        _escapeBaseSpin.Value = q.EscapeBase;
        _escapeEndWeightSpin.Value = q.EscapeEndWeight;
        _escapeWisWeightSpin.Value = q.EscapeWisWeight;

        _wfMinSpin.Value = q.WoundFailDays.Min;
        _wfMaxSpin.Value = q.WoundFailDays.Max;

        _wsChanceSpin.Value = q.WoundSuccess.Chance;
        _wsMinSpin.Value = q.WoundSuccess.Min;
        _wsMaxSpin.Value = q.WoundSuccess.Max;

        _nightAttackChanceSpin.Value = q.NightAttackChanceBase;
        _maxQuestDaysSpin.Value = q.MaxQuestDays;
        _nightCreatureLvlPenaltySpin.Value = q.NightCreatureLevelPenalty;

        _endInjuryKSpin.Value = q.EnduranceInjuryK;
        _endInjuryCapSpin.Value = q.EnduranceInjuryCap;
        _endDeathKSpin.Value = q.EnduranceDeathK;
        _endDeathCapSpin.Value = q.EnduranceDeathCap;

        _expContributionBaseScoreSpin.Value = q.Experience?.ContributionBaseScore ?? 0.2;
    }

    private void SyncFromUI()
    {
        var q = _quest;

        q.DurationMultiplier = NumericHelpers.Round4(_durMultiplierSpin.Value);
        q.DurationFailPenaltyPerPhase = NumericHelpers.Round4(_durFailPenaltySpin.Value);
        q.DurationIntBonusCap = NumericHelpers.Round4(_durIntBonusCapSpin.Value);
        q.DurationIntBonusK = NumericHelpers.Round4(_durIntBonusKSpin.Value);

        q.SkillRoll = new SkillRollBalance
        {
            Base = NumericHelpers.Round4(_skillBaseSpin.Value),
            PerRatio = NumericHelpers.Round4(_skillPerRatioSpin.Value),
            Min = NumericHelpers.Round4(_skillMinSpin.Value),
            Max = NumericHelpers.Round4(_skillMaxSpin.Value),
        };

        q.EscapeStatK = NumericHelpers.Round4(_escapeStatKSpin.Value);
        q.EscapeStatCap = NumericHelpers.Round4(_escapeStatCapSpin.Value);
        q.EscapeBase = NumericHelpers.Round4(_escapeBaseSpin.Value);
        q.EscapeEndWeight = NumericHelpers.Round4(_escapeEndWeightSpin.Value);
        q.EscapeWisWeight = NumericHelpers.Round4(_escapeWisWeightSpin.Value);

        q.WoundFailDays = new WoundRange
        {
            Min = (int)_wfMinSpin.Value,
            Max = (int)_wfMaxSpin.Value,
        };
        q.WoundSuccess = new WoundSuccessRange
        {
            Chance = (int)_wsChanceSpin.Value,
            Min = (int)_wsMinSpin.Value,
            Max = (int)_wsMaxSpin.Value,
        };

        q.NightAttackChanceBase = NumericHelpers.Round4(_nightAttackChanceSpin.Value);
        q.MaxQuestDays = (int)_maxQuestDaysSpin.Value;
        q.NightCreatureLevelPenalty = (int)_nightCreatureLvlPenaltySpin.Value;

        q.EnduranceInjuryK = NumericHelpers.Round4(_endInjuryKSpin.Value);
        q.EnduranceInjuryCap = NumericHelpers.Round4(_endInjuryCapSpin.Value);
        q.EnduranceDeathK = NumericHelpers.Round4(_endDeathKSpin.Value);
        q.EnduranceDeathCap = NumericHelpers.Round4(_endDeathCapSpin.Value);

        q.Experience = new ExperienceBalance
        {
            ContributionBaseScore = NumericHelpers.Round4(_expContributionBaseScoreSpin.Value),
        };
    }

    // ==================== Apply / Revert ====================

    private void OnApplyPressed()
    {
        if (_quest == null) { SetStatus("Профиль не загружен", StatusKind.Error); return; }

        SyncFromUI();

        if (TuningDock.PreviewEnabled)
        {
            try
            {
                QuestBalance.SetActiveProfile(_quest);
                SetStatus("Preview: профиль в памяти (диск не тронут).", StatusKind.Warning);
            }
            catch (Exception e)
            {
                SetStatus($"Preview FAILED: {e.Message}", StatusKind.Error);
                GD.PushError($"[BalanceTab] Preview: {e}");
            }
            return;
        }

        try
        {
            JsonWriter.Write(QuestBalancePath, _db);
        }
        catch (Exception e)
        {
            SetStatus($"Apply FAILED: {e.Message}", StatusKind.Error);
            GD.PushError($"[BalanceTab] Apply: {e}");
            return;
        }

        try
        {
            QuestBalance.Load();
            SetStatus($"Applied: QuestBalance записан ({_db.Profiles.Count} профилей)", StatusKind.Ok);
        }
        catch (Exception e)
        {
            SetStatus($"Записано, но Load упал: {e.Message}", StatusKind.Error);
            GD.PushError($"[BalanceTab] Load after apply: {e}");
        }
    }

    private void OnRevertPressed()
    {
        if (!ReloadFromDisk()) return;
        LoadIntoUI();
        try
        {
            QuestBalance.Load();
            SetStatus($"Reverted: профиль '{_quest.Id}' перезагружен", StatusKind.Ok);
        }
        catch (Exception e)
        {
            SetStatus($"Revert: Load упал: {e.Message}", StatusKind.Error);
        }
    }

    // ==================== Прочее ====================

    private bool ReloadFromDisk()
    {
        try
        {
            _db = JsonLoader.Load<QuestBalanceDatabase>(QuestBalancePath);

            if (_db?.Profiles == null || _db.Profiles.Count == 0)
            {
                GD.PushError($"[BalanceTab] {QuestBalancePath}: нет профилей");
                return false;
            }

            _quest = _db.Profiles.FirstOrDefault(p => p.Id == "default") ?? _db.Profiles[0];

            GD.Print($"[BalanceTab] ReloadFromDisk: профиль '{_quest.Id}', " +
                     $"{_db.Profiles.Count} шт. в базе");
            return true;
        }
        catch (Exception e)
        {
            GD.PushError($"[BalanceTab] ReloadFromDisk: {e}");
            return false;
        }
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