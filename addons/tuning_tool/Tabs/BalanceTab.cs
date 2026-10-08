#if TOOLS
using System;
using System.Collections.Generic;
using Godot;

[Tool]
public partial class BalanceTab : Control
{
    private const string QuestBalancePath = "res://Resources/Data/Quest/QuestBalance.json";
    private const string AdventurerBalancePath = "res://Resources/Data/Adventurer/AdventurerBalance.json";

    private QuestBalanceDatabase _questDb;
    private QuestBalanceProfile _quest;
    private AdventurerBalanceProfile _adv;

    private Label _status;

    // ===== Quest widgets =====
    private LineEdit _qId;
    private SpinBox _qDurEasy, _qDurNormal, _qDurHard;
    private SpinBox _qDurFailPenalty, _qDurIntBonusCap, _qDurIntBonusK;
    private SpinBox _qSkillBase, _qSkillPerRatio, _qSkillMin, _qSkillMax;
    private SpinBox _qEscapeStatK, _qEscapeStatCap;
    private SpinBox _qEscapeEasy, _qEscapeNormal, _qEscapeHard;
    private SpinBox _qEscapeEndWeight, _qEscapeWisWeight;
    private SpinBox _qWFailEasyMin, _qWFailEasyMax;
    private SpinBox _qWFailNormalMin, _qWFailNormalMax;
    private SpinBox _qWFailHardMin, _qWFailHardMax;
    private SpinBox _qWSuccEasyChance, _qWSuccEasyMin, _qWSuccEasyMax;
    private SpinBox _qWSuccNormalChance, _qWSuccNormalMin, _qWSuccNormalMax;
    private SpinBox _qWSuccHardChance, _qWSuccHardMin, _qWSuccHardMax;
    private SpinBox _qNightAttackChance, _qMaxQuestDays, _qNightCreatureLvlPenalty;
    private SpinBox _qEndInjuryK, _qEndInjuryCap, _qEndDeathK, _qEndDeathCap;
    private SpinBox _qExpContributionBaseScore;

    // ===== Adventurer widgets =====
    private SpinBox _aMaxLevel, _aExpCapLvl1;
    private OptionButton _aExpCapScaling;
    private SpinBox _aExpCapAcceleration;
    private SpinBox _aStatBaseValue, _aStatStartPool, _aStatBaseSlope;

    private enum StatusKind { Ok, Warning, Error }
    private static readonly string[] ScalingOptions =
        { "flat", "linear", "polynomial", "quadratic", "exponential" };

    public override void _Ready()
    {
        GD.Print("[BalanceTab] _Ready вызван");

        if (!ReloadFromDisk())
        {
            _status = new Label { Text = "Не удалось загрузить balance-файлы (см. Output)" };
            AddChild(_status);
            return;
        }

        var main = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        main.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(main);

        var top = new HBoxContainer();
        var applyBtn = new Button { Text = "Apply" };
        var revertBtn = new Button { Text = "Revert" };
        applyBtn.Pressed += OnApplyPressed;
        revertBtn.Pressed += OnRevertPressed;
        top.AddChild(applyBtn);
        top.AddChild(revertBtn);
        _status = new Label
        {
            Text = "",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        top.AddChild(_status);
        main.AddChild(top);
        main.AddChild(new HSeparator());

        var columns = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        main.AddChild(columns);

        BuildQuestColumn(columns);
        BuildAdventurerColumn(columns);

        LoadQuestIntoUI();
        LoadAdventurerIntoUI();
        SetStatus("Готово", StatusKind.Ok);
    }

    // ==================== Левая колонка: QuestBalance ====================

    private void BuildQuestColumn(Control parent)
    {
        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        parent.AddChild(scroll);

        var v = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(v);

        Section(v, "Quest Balance — профиль");
        _qId = TextRow(v, "id (read-only)", null);
        _qId.Editable = false;

        Section(v, "Duration");
        var t3 = Tier3Row(v, 0.0, 10.0, 0.05, 1.0, 1.2, 1.5,
            "Множитель длительности фазы по тиру: easy/normal/hard.");
        _qDurEasy = t3[0]; _qDurNormal = t3[1]; _qDurHard = t3[2];
        _qDurFailPenalty = SpinRow(v, "failPenaltyPerPhase", 0, 1, 0.01, 0.30,
            "Штраф к длительности за каждую проваленную фазу (доля).");
        _qDurIntBonusCap = SpinRow(v, "intBonusCap", 0, 5, 0.01, 0.30,
            "Максимальный бонус длительности от INT (доля).");
        _qDurIntBonusK = SpinRow(v, "intBonusK", 0, 5, 0.01, 0.15,
            "Коэффициент перевода INT в бонус длительности.");

        Section(v, "Skill Roll");
        _qSkillBase = SpinRow(v, "base", 0, 200, 1, 50);
        _qSkillPerRatio = SpinRow(v, "perRatio", 0, 200, 1, 50);
        _qSkillMin = SpinRow(v, "min", 0, 100, 1, 5);
        _qSkillMax = SpinRow(v, "max", 0, 100, 1, 80);

        Section(v, "Escape");
        _qEscapeStatK = SpinRow(v, "escapeStatK", 0, 200, 1, 40);
        _qEscapeStatCap = SpinRow(v, "escapeStatCap", 0, 100, 1, 95);
        var eb = Tier3Row(v, 0, 100, 1, 60, 45, 30, "Базовый шанс побега по тиру существа.");
        _qEscapeEasy = eb[0]; _qEscapeNormal = eb[1]; _qEscapeHard = eb[2];
        _qEscapeEndWeight = SpinRow(v, "endWeight", 0, 2, 0.01, 0.8);
        _qEscapeWisWeight = SpinRow(v, "wisWeight", 0, 2, 0.01, 0.5);

        Section(v, "Wound on fail (дней)");
        (_qWFailEasyMin, _qWFailEasyMax) = RangeRow(v, "easy", 2, 4);
        (_qWFailNormalMin, _qWFailNormalMax) = RangeRow(v, "normal", 3, 6);
        (_qWFailHardMin, _qWFailHardMax) = RangeRow(v, "hard", 5, 10);

        Section(v, "Wound on success");
        (_qWSuccEasyChance, _qWSuccEasyMin, _qWSuccEasyMax) = SuccessRow(v, "easy", 5, 1, 2);
        (_qWSuccNormalChance, _qWSuccNormalMin, _qWSuccNormalMax) = SuccessRow(v, "normal", 15, 1, 3);
        (_qWSuccHardChance, _qWSuccHardMin, _qWSuccHardMax) = SuccessRow(v, "hard", 30, 2, 4);

        Section(v, "Night / Limits");
        _qNightAttackChance = SpinRow(v, "nightAttackChanceBase", 0, 100, 1, 25);
        _qMaxQuestDays = SpinRow(v, "maxQuestDays", 1, 3650, 1, 90);
        _qNightCreatureLvlPenalty = SpinRow(v, "nightCreatureLevelPenalty", 0, 100, 1, 5);

        Section(v, "Endurance");
        _qEndInjuryK = SpinRow(v, "enduranceInjuryK", 0, 1000, 1, 60);
        _qEndInjuryCap = SpinRow(v, "enduranceInjuryCap", 0, 1, 0.01, 0.75);
        _qEndDeathK = SpinRow(v, "enduranceDeathK", 0, 1000, 1, 150);
        _qEndDeathCap = SpinRow(v, "enduranceDeathCap", 0, 1, 0.01, 0.50);

        Section(v, "Experience");
        _qExpContributionBaseScore = SpinRow(v, "contributionBaseScore", 0, 10, 0.01, 0.2);
    }

    // ==================== Правая колонка: AdventurerBalance ====================

    private void BuildAdventurerColumn(Control parent)
    {
        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        parent.AddChild(scroll);

        var v = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(v);

        Section(v, "Adventurer Balance");
        _aMaxLevel = SpinRow(v, "maxLevel", 1, 1000, 1, 60);
        _aExpCapLvl1 = SpinRow(v, "expCapLvl1", 1, 100000, 1, 45);

        var row = new HBoxContainer();
        row.AddChild(new Label { Text = "expCapScaling", CustomMinimumSize = new Vector2(180, 0) });
        _aExpCapScaling = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (var s in ScalingOptions) _aExpCapScaling.AddItem(s);
        row.AddChild(_aExpCapScaling);
        v.AddChild(row);

        _aExpCapAcceleration = SpinRow(v, "expCapAcceleration", 0, 10, 0.01, 0.22);

        Section(v, "Stats");
        _aStatBaseValue = SpinRow(v, "statBaseValue", 0, 1000, 0.01, 0.0);
        _aStatStartPool = SpinRow(v, "statStartPool", 0, 1000, 0.01, 10.0);
        _aStatBaseSlope = SpinRow(v, "statBaseSlope", 0, 1000, 0.01, 3.5);
    }

    // ==================== Хелперы UI ====================

    private static void Section(Control parent, string title)
    {
        parent.AddChild(new HSeparator());
        parent.AddChild(new Label { Text = title });
    }

    private static LineEdit TextRow(Control parent, string label, string tooltip)
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

    private static SpinBox[] Tier3Row(Control parent, double min, double max, double step,
                                      double vEasy, double vNormal, double vHard,
                                      string tooltip = null)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        string[] names = { "easy", "normal", "hard" };
        double[] vals = { vEasy, vNormal, vHard };
        var spins = new SpinBox[3];
        for (int i = 0; i < 3; i++)
        {
            row.AddChild(new Label { Text = names[i], TooltipText = tooltip ?? "" });
            spins[i] = new SpinBox
            {
                MinValue = min,
                MaxValue = max,
                Step = step,
                Value = vals[i],
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            row.AddChild(spins[i]);
        }
        parent.AddChild(row);
        return spins;
    }

    private static (SpinBox min, SpinBox max) RangeRow(Control parent, string label, int vMin, int vMax)
    {
        var row = new HBoxContainer();
        row.AddChild(new Label { Text = label, CustomMinimumSize = new Vector2(70, 0) });
        row.AddChild(new Label { Text = "min" });
        var mn = new SpinBox
        {
            MinValue = 0,
            MaxValue = 10000,
            Step = 1,
            Value = vMin,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        row.AddChild(mn);
        row.AddChild(new Label { Text = "max" });
        var mx = new SpinBox
        {
            MinValue = 0,
            MaxValue = 10000,
            Step = 1,
            Value = vMax,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        row.AddChild(mx);
        parent.AddChild(row);
        return (mn, mx);
    }

    private static (SpinBox chance, SpinBox min, SpinBox max) SuccessRow(
        Control parent, string label, int chance, int vMin, int vMax)
    {
        var row = new HBoxContainer();
        row.AddChild(new Label { Text = label, CustomMinimumSize = new Vector2(70, 0) });
        row.AddChild(new Label { Text = "chance%" });
        var ch = new SpinBox
        {
            MinValue = 0,
            MaxValue = 100,
            Step = 1,
            Value = chance,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        row.AddChild(ch);
        row.AddChild(new Label { Text = "min" });
        var mn = new SpinBox
        {
            MinValue = 0,
            MaxValue = 1000,
            Step = 1,
            Value = vMin,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        row.AddChild(mn);
        row.AddChild(new Label { Text = "max" });
        var mx = new SpinBox
        {
            MinValue = 0,
            MaxValue = 1000,
            Step = 1,
            Value = vMax,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        row.AddChild(mx);
        parent.AddChild(row);
        return (ch, mn, mx);
    }

    // ==================== Load disk -> UI ====================

    private void LoadQuestIntoUI()
    {
        var q = _quest;
        _qId.Text = q.Id ?? "";

        _qDurEasy.Value = q.DurationTierMultiplier.Easy;
        _qDurNormal.Value = q.DurationTierMultiplier.Normal;
        _qDurHard.Value = q.DurationTierMultiplier.Hard;
        _qDurFailPenalty.Value = q.DurationFailPenaltyPerPhase;
        _qDurIntBonusCap.Value = q.DurationIntBonusCap;
        _qDurIntBonusK.Value = q.DurationIntBonusK;

        _qSkillBase.Value = q.SkillRoll.Base;
        _qSkillPerRatio.Value = q.SkillRoll.PerRatio;
        _qSkillMin.Value = q.SkillRoll.Min;
        _qSkillMax.Value = q.SkillRoll.Max;

        _qEscapeStatK.Value = q.EscapeStatK;
        _qEscapeStatCap.Value = q.EscapeStatCap;
        _qEscapeEasy.Value = q.EscapeBaseByTier.Easy;
        _qEscapeNormal.Value = q.EscapeBaseByTier.Normal;
        _qEscapeHard.Value = q.EscapeBaseByTier.Hard;
        _qEscapeEndWeight.Value = q.EscapeEndWeight;
        _qEscapeWisWeight.Value = q.EscapeWisWeight;

        _qWFailEasyMin.Value = q.WoundFailDaysByTier.Easy.Min;
        _qWFailEasyMax.Value = q.WoundFailDaysByTier.Easy.Max;
        _qWFailNormalMin.Value = q.WoundFailDaysByTier.Normal.Min;
        _qWFailNormalMax.Value = q.WoundFailDaysByTier.Normal.Max;
        _qWFailHardMin.Value = q.WoundFailDaysByTier.Hard.Min;
        _qWFailHardMax.Value = q.WoundFailDaysByTier.Hard.Max;

        _qWSuccEasyChance.Value = q.WoundSuccessByTier.Easy.Chance;
        _qWSuccEasyMin.Value = q.WoundSuccessByTier.Easy.Min;
        _qWSuccEasyMax.Value = q.WoundSuccessByTier.Easy.Max;
        _qWSuccNormalChance.Value = q.WoundSuccessByTier.Normal.Chance;
        _qWSuccNormalMin.Value = q.WoundSuccessByTier.Normal.Min;
        _qWSuccNormalMax.Value = q.WoundSuccessByTier.Normal.Max;
        _qWSuccHardChance.Value = q.WoundSuccessByTier.Hard.Chance;
        _qWSuccHardMin.Value = q.WoundSuccessByTier.Hard.Min;
        _qWSuccHardMax.Value = q.WoundSuccessByTier.Hard.Max;

        _qNightAttackChance.Value = q.NightAttackChanceBase;
        _qMaxQuestDays.Value = q.MaxQuestDays;
        _qNightCreatureLvlPenalty.Value = q.NightCreatureLevelPenalty;

        _qEndInjuryK.Value = q.EnduranceInjuryK;
        _qEndInjuryCap.Value = q.EnduranceInjuryCap;
        _qEndDeathK.Value = q.EnduranceDeathK;
        _qEndDeathCap.Value = q.EnduranceDeathCap;

        _qExpContributionBaseScore.Value = q.Experience?.ContributionBaseScore ?? 0.2;
    }

    private void LoadAdventurerIntoUI()
    {
        var a = _adv;
        _aMaxLevel.Value = a.MaxLevel;
        _aExpCapLvl1.Value = a.ExpCapLvl1;

        int idx = Array.IndexOf(ScalingOptions, a.ExpCapScaling);
        _aExpCapScaling.Selected = idx >= 0 ? idx : Array.IndexOf(ScalingOptions, "polynomial");

        _aExpCapAcceleration.Value = a.ExpCapAcceleration;
        _aStatBaseValue.Value = a.StatBaseValue;
        _aStatStartPool.Value = a.StatStartPool;
        _aStatBaseSlope.Value = a.StatBaseSlope;
    }

    // ==================== Sync UI -> модели ====================

    private void SyncQuestFromUI()
    {
        var q = _quest;

        q.DurationTierMultiplier = new TierMultiplierMap
        {
            Easy = NumericHelpers.Round4(_qDurEasy.Value),
            Normal = NumericHelpers.Round4(_qDurNormal.Value),
            Hard = NumericHelpers.Round4(_qDurHard.Value),
        };
        q.DurationFailPenaltyPerPhase = NumericHelpers.Round4(_qDurFailPenalty.Value);
        q.DurationIntBonusCap = NumericHelpers.Round4(_qDurIntBonusCap.Value);
        q.DurationIntBonusK = NumericHelpers.Round4(_qDurIntBonusK.Value);

        q.SkillRoll = new SkillRollBalance
        {
            Base = NumericHelpers.Round4(_qSkillBase.Value),
            PerRatio = NumericHelpers.Round4(_qSkillPerRatio.Value),
            Min = NumericHelpers.Round4(_qSkillMin.Value),
            Max = NumericHelpers.Round4(_qSkillMax.Value),
        };

        q.EscapeStatK = NumericHelpers.Round4(_qEscapeStatK.Value);
        q.EscapeStatCap = NumericHelpers.Round4(_qEscapeStatCap.Value);
        q.EscapeBaseByTier = new EscapeBaseByTier
        {
            Easy = NumericHelpers.Round4(_qEscapeEasy.Value),
            Normal = NumericHelpers.Round4(_qEscapeNormal.Value),
            Hard = NumericHelpers.Round4(_qEscapeHard.Value),
        };
        q.EscapeEndWeight = NumericHelpers.Round4(_qEscapeEndWeight.Value);
        q.EscapeWisWeight = NumericHelpers.Round4(_qEscapeWisWeight.Value);

        q.WoundFailDaysByTier = new WoundDaysByTier
        {
            Easy = new WoundRange { Min = (int)_qWFailEasyMin.Value, Max = (int)_qWFailEasyMax.Value },
            Normal = new WoundRange { Min = (int)_qWFailNormalMin.Value, Max = (int)_qWFailNormalMax.Value },
            Hard = new WoundRange { Min = (int)_qWFailHardMin.Value, Max = (int)_qWFailHardMax.Value },
        };
        q.WoundSuccessByTier = new WoundSuccessByTier
        {
            Easy = new WoundSuccessRange { Chance = (int)_qWSuccEasyChance.Value, Min = (int)_qWSuccEasyMin.Value, Max = (int)_qWSuccEasyMax.Value },
            Normal = new WoundSuccessRange { Chance = (int)_qWSuccNormalChance.Value, Min = (int)_qWSuccNormalMin.Value, Max = (int)_qWSuccNormalMax.Value },
            Hard = new WoundSuccessRange { Chance = (int)_qWSuccHardChance.Value, Min = (int)_qWSuccHardMin.Value, Max = (int)_qWSuccHardMax.Value },
        };

        q.NightAttackChanceBase = NumericHelpers.Round4(_qNightAttackChance.Value);
        q.MaxQuestDays = (int)_qMaxQuestDays.Value;
        q.NightCreatureLevelPenalty = (int)_qNightCreatureLvlPenalty.Value;

        q.EnduranceInjuryK = NumericHelpers.Round4(_qEndInjuryK.Value);
        q.EnduranceInjuryCap = NumericHelpers.Round4(_qEndInjuryCap.Value);
        q.EnduranceDeathK = NumericHelpers.Round4(_qEndDeathK.Value);
        q.EnduranceDeathCap = NumericHelpers.Round4(_qEndDeathCap.Value);

        q.Experience = new ExperienceBalance
        {
            ContributionBaseScore = NumericHelpers.Round4(_qExpContributionBaseScore.Value),
        };
    }

    private void SyncAdventurerFromUI()
    {
        var a = _adv;
        a.MaxLevel = (int)_aMaxLevel.Value;
        a.ExpCapLvl1 = (int)_aExpCapLvl1.Value;
        if (_aExpCapScaling.Selected >= 0 && _aExpCapScaling.Selected < ScalingOptions.Length)
            a.ExpCapScaling = ScalingOptions[_aExpCapScaling.Selected];
        a.ExpCapAcceleration = NumericHelpers.Round4(_aExpCapAcceleration.Value);
        a.StatBaseValue = NumericHelpers.Round4(_aStatBaseValue.Value);
        a.StatStartPool = NumericHelpers.Round4(_aStatStartPool.Value);
        a.StatBaseSlope = NumericHelpers.Round4(_aStatBaseSlope.Value);
    }

    // ==================== Apply / Revert ====================

    private void OnApplyPressed()
    {
        SyncQuestFromUI();
        SyncAdventurerFromUI();

        try
        {
            JsonWriter.Write(QuestBalancePath, _questDb);
            JsonWriter.Write(AdventurerBalancePath, _adv);
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
            AdventurerBalance.Load();
            SetStatus($"Applied: QuestBalance + AdventurerBalance записаны",
                      StatusKind.Ok);
        }
        catch (Exception e)
        {
            SetStatus($"Записано, но Load упал: {e.Message}", StatusKind.Error);
            GD.PushError($"[BalanceTab] Load after apply: {e}");
        }
    }

    private void OnRevertPressed()
    {
        ReloadFromDisk();
        LoadQuestIntoUI();
        LoadAdventurerIntoUI();
        try
        {
            QuestBalance.Load();
            AdventurerBalance.Load();
            SetStatus("Reverted: QuestBalance + AdventurerBalance перезагружены",
                      StatusKind.Ok);
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
            _questDb = JsonLoader.Load<QuestBalanceDatabase>(QuestBalancePath);
            _adv = JsonLoader.Load<AdventurerBalanceProfile>(AdventurerBalancePath);

            if (_questDb?.Profiles == null || _questDb.Profiles.Count == 0)
            {
                GD.PushError($"[BalanceTab] {QuestBalancePath}: нет профилей");
                return false;
            }

            // Берём профиль с id="default", если он есть, иначе первый.
            _quest = null;
            foreach (var p in _questDb.Profiles)
                if (p.Id == "default") { _quest = p; break; }
            _quest ??= _questDb.Profiles[0];

            GD.Print($"[BalanceTab] ReloadFromDisk: профиль '{_quest.Id}', " +
                     $"scaling='{_adv.ExpCapScaling}', maxLevel={_adv.MaxLevel}");
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