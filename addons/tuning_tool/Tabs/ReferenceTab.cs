#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Data.Adventurer;
using AdventurersGuild.Data.Balance;
using AdventurersGuild.Data.Quest;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Quests;
using Godot;

[Tool]
public partial class ReferenceTab : Control
{
    private const float ColPhase = 110;
    private const float ColSol = 40;
    private const float ColSkill = 140;
    private const float ColRawDc = 80;
    private const float ColChance = 130;
    private const float ColDelta = 100;

    private HBoxContainer _comparisonTableBox;
    private Label _refInfoLabel;
    private CheckBox _showRestCheck;
    private OptionButton _classOption;
    private OptionButton _raceOption;
    private SpinBox _comparisonLevelSpin;

    private readonly List<string> _classIds = new();
    private readonly List<string> _raceIds = new();

    public override void _Ready()
    {
        GD.Print("[ReferenceTab] _Ready вызван");

        var root = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        AddChild(root);
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        BuildComparisonUI(root);

        RefreshComparison();
    }

    private void BuildComparisonUI(Control parent)
    {
        parent.AddChild(new Label
        {
            Text = "Сравнение текущих rawDc в QuestPhases.json с эталоном 50% " +
                   "для выбранного класса/расы.\n" +
                   "Δ < 0 — фаза легче эталона; Δ > 0 — тяжелее.",
            Modulate = new Color(0.7f, 0.7f, 0.7f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });
        parent.AddChild(new HSeparator());

        var topRow = new HBoxContainer();
        topRow.AddChild(new Label { Text = "Class", CustomMinimumSize = new Vector2(50, 0) });
        _classOption = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _classOption.ItemSelected += _ => RefreshComparison();
        topRow.AddChild(_classOption);

        topRow.AddChild(new Label { Text = "Race", CustomMinimumSize = new Vector2(50, 0) });
        _raceOption = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _raceOption.ItemSelected += _ => RefreshComparison();
        topRow.AddChild(_raceOption);

        topRow.AddChild(new Label { Text = "Level", CustomMinimumSize = new Vector2(50, 0) });
        _comparisonLevelSpin = new SpinBox
        {
            MinValue = 1,
            MaxValue = 60,
            Step = 1,
            Value = 1,
            CustomMinimumSize = new Vector2(90, 0),
        };
        _comparisonLevelSpin.ValueChanged += _ => RefreshComparison();
        topRow.AddChild(_comparisonLevelSpin);
        parent.AddChild(topRow);

        var topRow2 = new HBoxContainer();
        _showRestCheck = new CheckBox
        {
            Text = "Show rest phases (short_rest, long_rest)",
            ButtonPressed = false,
        };
        _showRestCheck.Toggled += _ => RefreshComparison();
        topRow2.AddChild(_showRestCheck);

        var refreshBtn = new Button { Text = "Refresh" };
        refreshBtn.Pressed += RefreshComparison;
        topRow2.AddChild(refreshBtn);
        parent.AddChild(topRow2);

        PopulateClassRaceOptions();

        _refInfoLabel = new Label { Text = "" };
        parent.AddChild(_refInfoLabel);

        parent.AddChild(new HSeparator());

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        parent.AddChild(scroll);

        _comparisonTableBox = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        scroll.AddChild(_comparisonTableBox);
    }

    private void PopulateClassRaceOptions()
    {
        _classIds.Clear();
        _classOption.Clear();
        foreach (var c in AdventurerDatabase.Classes.Values.OrderBy(x => x.Id))
        {
            _classIds.Add(c.Id);
            _classOption.AddItem(c.Id);
        }
        if (_classIds.Count > 0) _classOption.Selected = 0;

        _raceIds.Clear();
        _raceOption.Clear();
        foreach (var r in AdventurerDatabase.Races.Values.OrderBy(x => x.Id))
        {
            _raceIds.Add(r.Id);
            _raceOption.AddItem(r.Id);
        }
        if (_raceIds.Count > 0) _raceOption.Selected = 0;
    }

    private void RefreshComparison()
    {
        if (_comparisonTableBox == null) return;

        foreach (var child in _comparisonTableBox.GetChildren())
            child.QueueFree();

        if (_classIds.Count == 0 || _raceIds.Count == 0) return;

        string classId = _classIds[Math.Max(0, _classOption.Selected)];
        string raceId = _raceIds[Math.Max(0, _raceOption.Selected)];
        int level = (int)_comparisonLevelSpin.Value;

        var b = QuestBalance.Active.SkillRoll;
        var ab = AdventurerBalance.Active;

        double refMax = ab.StatStartPool + (level - 1) * ab.StatBaseSlope;
        var stats = StatGrowth.AtLevel(classId, raceId, level);

        _refInfoLabel.Text =
            $"class={classId}  race={raceId}  level={level}  refMax={refMax:F2}  " +
            $"base={b.Base:F0}  perRatio={b.PerRatio:F0}  min={b.Min:F0}  max={b.Max:F0}";

        bool showRest = _showRestCheck?.ButtonPressed ?? false;
        var phases = QuestDatabase.Phases.Values
            .Where(p => showRest || (p.Id != "short_rest" && p.Id != "long_rest"))
            .OrderBy(p => p.Id)
            .ToList();

        var rows = new List<RowData>();
        foreach (var phase in phases)
        {
            if (phase.Solutions == null) continue;

            for (int si = 0; si < phase.Solutions.Count; si++)
            {
                var sol = phase.Solutions[si];
                if (sol?.Skills == null) continue;

                foreach (var kv in sol.Skills)
                    rows.Add(ComputeRow(phase.Id, si, kv.Key, kv.Value,
                                        classId, stats, refMax, b));
            }
        }

        var colPhase = BuildColumn("phase", ColPhase);
        var colSol = BuildColumn("sol", ColSol);
        var colSkill = BuildColumn("skill", ColSkill);
        var colRawDc = BuildColumn("rawDc", ColRawDc);
        var colChance = BuildColumn("chance%", ColChance);
        var colDelta = BuildColumn("Δ vs 50%", ColDelta);

        _comparisonTableBox.AddChild(colPhase);
        _comparisonTableBox.AddChild(colSol);
        _comparisonTableBox.AddChild(colSkill);
        _comparisonTableBox.AddChild(colRawDc);
        _comparisonTableBox.AddChild(colChance);
        _comparisonTableBox.AddChild(colDelta);

        foreach (var r in rows)
        {
            colPhase.AddChild(Cell(r.PhaseId, ColPhase, new Color(1f, 1f, 1f)));
            colSol.AddChild(Cell(r.SolutionIndex.ToString(), ColSol, new Color(0.75f, 0.75f, 0.75f)));
            colSkill.AddChild(Cell(r.SkillId, ColSkill, new Color(1f, 1f, 1f)));
            colRawDc.AddChild(Cell(r.RawDc.ToString("F3"), ColRawDc, new Color(1f, 1f, 1f)));

            var chanceCell = Cell(r.Chance.ToString("F1") + "%", ColChance, new Color(1f, 1f, 1f));
            chanceCell.TooltipText =
                $"skillValue = {r.SkillValue:F2}\n" +
                $"required   = {r.Required:F2}\n" +
                $"ratio      = {r.Ratio:F3}\n" +
                $"chance     = clamp({b.Base:F0} + ({r.Ratio:F3} − 1)·{b.PerRatio:F0}, " +
                $"{b.Min:F0}..{b.Max:F0}) = {r.Chance:F1}%";
            colChance.AddChild(chanceCell);

            var deltaColor = Math.Abs(r.Delta) < 0.05
                ? new Color(0.6f, 1f, 0.6f)
                : (r.Delta < 0 ? new Color(1f, 0.85f, 0.4f) : new Color(1f, 0.6f, 0.6f));
            colDelta.AddChild(Cell(r.Delta.ToString("+0.000;-0.000;0.000"), ColDelta, deltaColor));
        }

        colPhase.AddChild(Cell("", ColPhase, new Color(1f, 1f, 1f)));
        colSol.AddChild(Cell("", ColSol, new Color(1f, 1f, 1f)));
        colSkill.AddChild(Cell($"{rows.Count} rows", ColSkill, new Color(0.7f, 0.7f, 0.7f)));
    }

    private sealed class RowData
    {
        public string PhaseId;
        public int SolutionIndex;
        public string SkillId;
        public double RawDc;
        public double SkillValue;
        public double Required;
        public double Ratio;
        public double Chance;
        public double Delta;
    }

    private static RowData ComputeRow(string phaseId, int solIndex, string skillId, double rawDc, string classId, Dictionary<string, double> stats, double refMax, SkillRollBalance b)
    {
        double skillValue = 0;
        string statId = AdventurerDatabase.StatForSkill(skillId);
        if (statId != null && stats.TryGetValue(statId, out var statVal))
        {
            var skillInfo = AdventurerDatabase.GetSkill(skillId);
            double statMod = skillInfo?.StatModifier ?? 1.0;
            double classMod = 1.0;
            if (skillInfo?.ClassModifiers != null
                && skillInfo.ClassModifiers.TryGetValue(classId, out var cm))
                classMod = cm;

            skillValue = statVal * statMod * classMod;
        }

        double required = rawDc * refMax;
        double ratio = required > 0 ? skillValue / required : 0;
        double chance = Math.Clamp(b.Base + (ratio - 1.0) * b.PerRatio, b.Min, b.Max);

        double ratio50 = (50.0 - b.Base) / b.PerRatio + 1.0;
        double rawDc50 = (refMax > 0 && ratio50 > 0)
            ? skillValue / (refMax * ratio50)
            : 0;

        return new RowData
        {
            PhaseId = phaseId,
            SolutionIndex = solIndex,
            SkillId = skillId,
            RawDc = rawDc,
            SkillValue = skillValue,
            Required = required,
            Ratio = ratio,
            Chance = chance,
            Delta = rawDc - rawDc50,
        };
    }

    private static Label Cell(string text, float width, Color color)
    {
        return new Label
        {
            Text = text,
            CustomMinimumSize = new Vector2(width, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = color,
            AutoTranslateMode = AutoTranslateModeEnum.Disabled,
        };
    }

    private static VBoxContainer BuildColumn(string header, float width)
    {
        var v = new VBoxContainer { CustomMinimumSize = new Vector2(width, 0) };
        v.AddChild(Cell(header, width, new Color(1f, 0.95f, 0.7f)));
        return v;
    }
}
#endif