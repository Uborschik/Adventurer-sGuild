#if TOOLS
using System;
using AdventurersGuild.Core;
using AdventurersGuild.Data.Balance;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Domain.Adventurers;
using Godot;

[Tool]
public partial class ProgressionTab : Control
{
    private const string AdventurerBalancePath =
        "res://Resources/Data/Adventurer/AdventurerBalance.json";

    private const float LabelW = 160;
    private const float FormWidth = 750;

    private Label _status;

    private SpinBox _maxLevelSpin;
    private SpinBox _expCapLvl1Spin;
    private OptionButton _expCapScalingOption;
    private SpinBox _expCapAccelerationSpin;

    private SpinBox _statBaseValueSpin;
    private SpinBox _statStartPoolSpin;
    private SpinBox _statBaseSlopeSpin;

    private AdventurerBalanceProfile _profile;

    private enum StatusKind { Ok, Warning, Error }

    private static readonly string[] ScalingOptions =
        { "flat", "linear", "polynomial", "quadratic", "exponential" };

    public override void _Ready()
    {
        GD.Print("[ProgressionTab] _Ready вызван");

        var outerScroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        AddChild(outerScroll);
        outerScroll.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var root = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        root.AddThemeConstantOverride("separation", 10);
        outerScroll.AddChild(root);

        root.AddChild(new Label
        {
            Text = "Параметры прокачки авантюристов: скорость роста уровней и базовые " +
                   "значения статов на уровне.",
            Modulate = new Color(0.7f, 0.7f, 0.7f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });
        root.AddChild(new HSeparator());

        var (_, lvlBox) = CollapsibleSection(root, "Level progression", expanded: true);
        BuildLevelSection(lvlBox);

        var (_, statBox) = CollapsibleSection(root, "Stat growth base", expanded: true);
        BuildStatSection(statBox);

        var (_, previewBox) = CollapsibleSection(root, "Preview curve (1..60)", expanded: true);
        BuildPreviewSection(previewBox);

        root.AddChild(new HSeparator());

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

        ReloadFromDisk();
        LoadIntoUI();
    }

    // ==================== Level section ====================

    private void BuildLevelSection(Control parent)
    {
        var r1 = FormRow(parent);
        r1.AddChild(FormLabel("Max level"));
        _maxLevelSpin = SpinBox(1, 1000, 1, 60);
        r1.AddChild(_maxLevelSpin);

        r1.AddChild(FormLabel("Exp cap @ L1"));
        _expCapLvl1Spin = SpinBox(1, 100000, 1, 45);
        r1.AddChild(_expCapLvl1Spin);

        var r2 = FormRow(parent);
        r2.AddChild(FormLabel("Exp cap scaling"));
        _expCapScalingOption = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        foreach (var s in ScalingOptions) _expCapScalingOption.AddItem(s);
        r2.AddChild(_expCapScalingOption);

        r2.AddChild(FormLabel("Acceleration"));
        _expCapAccelerationSpin = SpinBox(0, 10, 0.01, 0.22);
        r2.AddChild(_expCapAccelerationSpin);

        parent.AddChild(new Label
        {
            Text = "Exp cap = expCapLvl1 × f(level, scaling, acceleration).\n" +
                   "  flat         = expCapLvl1\n" +
                   "  linear       = expCapLvl1 × L\n" +
                   "  polynomial   = expCapLvl1 × L × (1 + (L−1)·accel)\n" +
                   "  quadratic    = expCapLvl1 × L²\n" +
                   "  exponential  = expCapLvl1 × 1.5^(L−1)",
            Modulate = new Color(0.65f, 0.65f, 0.65f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });
    }

    // ==================== Stat section ====================

    private void BuildStatSection(Control parent)
    {
        var r1 = FormRow(parent);
        r1.AddChild(FormLabel("Stat base value"));
        _statBaseValueSpin = SpinBox(0, 1000, 0.01, 0);
        r1.AddChild(_statBaseValueSpin);

        r1.AddChild(FormLabel("Stat start pool"));
        _statStartPoolSpin = SpinBox(0, 1000, 0.01, 10);
        r1.AddChild(_statStartPoolSpin);

        var r2 = FormRow(parent);
        r2.AddChild(FormLabel("Stat base slope"));
        _statBaseSlopeSpin = SpinBox(0, 1000, 0.01, 3.5);
        r2.AddChild(_statBaseSlopeSpin);
        r2.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        parent.AddChild(new Label
        {
            Text = "refMax(L) = statStartPool + (L−1)·statBaseSlope\n" +
                   "statValue(class, stat, L) = statBaseValue + refMax(L)·growthWeight + raceBonus",
            Modulate = new Color(0.65f, 0.65f, 0.65f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });
    }

    // ==================== Preview ====================

    private HBoxContainer _previewTableBox;
    private static FontFile _monoFont;

    private void BuildPreviewSection(Control parent)
    {
        var btn = new Button { Text = "Recalculate", SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
        btn.Pressed += UpdatePreview;
        parent.AddChild(btn);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 260),
        };
        parent.AddChild(scroll);

        _previewTableBox = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        scroll.AddChild(_previewTableBox);
    }

    private void UpdatePreview()
    {
        if (_previewTableBox == null) return;

        foreach (var child in _previewTableBox.GetChildren())
            child.QueueFree();

        int maxLevel = (int)_maxLevelSpin.Value;
        int cap1 = (int)_expCapLvl1Spin.Value;
        string scaling = ScalingOptions[Math.Max(0, _expCapScalingOption.Selected)];
        double accel = _expCapAccelerationSpin.Value;

        double startPool = _statStartPoolSpin.Value;
        double slope = _statBaseSlopeSpin.Value;
        double baseVal = _statBaseValueSpin.Value;

        var colL = MonoColumn("L", PrevColL);
        var colExp = MonoColumn("expToNext", PrevColExp);
        var colRef = MonoColumn("refMax", PrevColRefMax);
        var colStat = MonoColumn("stat(w=1)", PrevColStat);

        _previewTableBox.AddChild(colL);
        _previewTableBox.AddChild(colExp);
        _previewTableBox.AddChild(colRef);
        _previewTableBox.AddChild(colStat);

        for (int L = 1; L <= Math.Min(maxLevel, 60); L++)
        {
            int exp = ExpCapFor(L, cap1, scaling, accel, maxLevel);
            double refMax = startPool + (L - 1) * slope;
            double stat = baseVal + refMax * 1.0;

            string expText = exp == int.MaxValue ? "inf" : exp.ToString();

            colL.AddChild(MonoCell(L.ToString(), PrevColL, new Color(0.85f, 0.85f, 0.85f)));
            colExp.AddChild(MonoCell(expText, PrevColExp, new Color(1f, 1f, 1f)));
            colRef.AddChild(MonoCell(refMax.ToString("F2"), PrevColRefMax, new Color(1f, 1f, 1f)));
            colStat.AddChild(MonoCell(stat.ToString("F2"), PrevColStat, new Color(1f, 1f, 1f)));
        }
    }

    private static int ExpCapFor(int level, int cap1, string scaling, double accel, int maxLevel)
    {
        if (level >= maxLevel) return int.MaxValue;
        if (level < 1) return cap1;

        double L = level;
        return scaling switch
        {
            "flat" => cap1,
            "linear" => cap1 * level,
            "polynomial" => (int)(cap1 * L * (1.0 + (L - 1.0) * accel)),
            "quadratic" => cap1 * level * level,
            "exponential" => (int)(cap1 * Math.Pow(1.5, level - 1)),
            _ => cap1 * level,
        };
    }

    // ==================== Disk <-> UI ====================

    private void ReloadFromDisk()
    {
        try
        {
            _profile = JsonLoader.Load<AdventurerBalanceProfile>(AdventurerBalancePath);
            GD.Print($"[ProgressionTab] ReloadFromDisk: maxLevel={_profile.MaxLevel}, " +
                     $"scaling='{_profile.ExpCapScaling}'");
        }
        catch (Exception e)
        {
            GD.PushError($"[ProgressionTab] Не удалось загрузить {AdventurerBalancePath}: {e}");
            _profile = new AdventurerBalanceProfile();
        }
    }

    private void LoadIntoUI()
    {
        _maxLevelSpin.Value = _profile.MaxLevel;
        _expCapLvl1Spin.Value = _profile.ExpCapLvl1;

        int idx = Array.IndexOf(ScalingOptions, _profile.ExpCapScaling);
        _expCapScalingOption.Selected = idx >= 0 ? idx : Array.IndexOf(ScalingOptions, "polynomial");

        _expCapAccelerationSpin.Value = _profile.ExpCapAcceleration;
        _statBaseValueSpin.Value = _profile.StatBaseValue;
        _statStartPoolSpin.Value = _profile.StatStartPool;
        _statBaseSlopeSpin.Value = _profile.StatBaseSlope;

        UpdatePreview();
    }

    private void SyncFromUI()
    {
        _profile.MaxLevel = (int)_maxLevelSpin.Value;
        _profile.ExpCapLvl1 = (int)_expCapLvl1Spin.Value;

        if (_expCapScalingOption.Selected >= 0
            && _expCapScalingOption.Selected < ScalingOptions.Length)
            _profile.ExpCapScaling = ScalingOptions[_expCapScalingOption.Selected];

        _profile.ExpCapAcceleration = NumericHelpers.Round4(_expCapAccelerationSpin.Value);
        _profile.StatBaseValue = NumericHelpers.Round4(_statBaseValueSpin.Value);
        _profile.StatStartPool = NumericHelpers.Round4(_statStartPoolSpin.Value);
        _profile.StatBaseSlope = NumericHelpers.Round4(_statBaseSlopeSpin.Value);
    }

    // ==================== Apply / Revert ====================

    private void OnApplyPressed()
    {
        SyncFromUI();

        if (TuningDock.PreviewEnabled)
        {
            try
            {
                AdventurerBalance.SetActiveProfile(_profile);
                SetStatus("Preview: профиль в памяти (диск не тронут).", StatusKind.Warning);
            }
            catch (Exception e)
            {
                SetStatus($"Preview FAILED: {e.Message}", StatusKind.Error);
                GD.PushError($"[ProgressionTab] Preview: {e}");
            }
            return;
        }

        try
        {
            JsonWriter.Write(AdventurerBalancePath, _profile);
        }
        catch (Exception e)
        {
            SetStatus($"Apply FAILED: {e.Message}", StatusKind.Error);
            GD.PushError($"[ProgressionTab] Apply: {e}");
            return;
        }

        try
        {
            AdventurerBalance.Load();
            SetStatus("Applied: AdventurerBalance записан", StatusKind.Ok);
        }
        catch (Exception e)
        {
            SetStatus($"Записано, но Load упал: {e.Message}", StatusKind.Error);
            GD.PushError($"[ProgressionTab] Load after apply: {e}");
        }
    }

    private void OnRevertPressed()
    {
        ReloadFromDisk();
        LoadIntoUI();
        try
        {
            AdventurerBalance.Load();
            SetStatus("Reverted: AdventurerBalance перезагружен", StatusKind.Ok);
        }
        catch (Exception e)
        {
            SetStatus($"Revert: Load упал: {e.Message}", StatusKind.Error);
        }
    }

    // ==================== UI-хелперы ====================

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
        var wrap = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
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

    private const float PrevColL = 70;
    private const float PrevColExp = 140;
    private const float PrevColRefMax = 120;
    private const float PrevColStat = 140;

    private static FontFile GetMonoFont()
    {
        if (_monoFont != null) return _monoFont;

        const string fontPath = "res://addons/tuning_tool/fonts/JetBrainsMono-Regular.ttf";
        if (!ResourceLoader.Exists(fontPath))
        {
            GD.PushWarning($"[ProgressionTab] Шрифт не найден: {fontPath}");
            return null;
        }

        _monoFont = ResourceLoader.Load<FontFile>(fontPath);
        return _monoFont;
    }

    private static Label MonoCell(string text, float width, Color color)
    {
        var lbl = new Label
        {
            Text = text,
            CustomMinimumSize = new Vector2(width, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = color,
            AutoTranslateMode = AutoTranslateModeEnum.Disabled,
        };

        var font = GetMonoFont();
        if (font != null) lbl.AddThemeFontOverride("font", font);

        return lbl;
    }

    private static VBoxContainer MonoColumn(string header, float width)
    {
        var v = new VBoxContainer { CustomMinimumSize = new Vector2(width, 0) };
        v.AddChild(MonoCell(header, width, new Color(1f, 0.95f, 0.7f)));
        return v;
    }
}
#endif