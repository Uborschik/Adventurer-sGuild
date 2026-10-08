#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

[Tool]
public partial class SimulatorTab : Control
{
    // Левый столбец: партия
    private VBoxContainer _partyListBox;
    private OptionButton _classOption;
    private OptionButton _raceOption;
    private SpinBox _levelSpin;

    // Правый столбец: квест + симуляция
    private OptionButton _templateOption;
    private OptionButton _creatureOption;
    private SpinBox _codeLevelSpin;
    private SpinBox _trialsSpin;
    private CheckBox _randomSeedCheck;
    private SpinBox _seedSpin;
    private RichTextLabel _output;

    // Модели и параллельные списки id для OptionButton индексов
    private readonly List<AdventurerModel> _party = new();
    private readonly List<string> _classIds = new();
    private readonly List<string> _raceIds = new();
    private readonly List<string> _templateIds = new();
    private readonly List<string> _creatureIds = new();

    public override void _Ready()
    {
        var main = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        main.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(main);

        BuildLeftColumn(main);
        BuildRightColumn(main);

        PopulateClassOptions();
        PopulateRaceOptions();
        PopulateTemplateOptions();
    }

    // ==================== UI: левая колонка ====================

    private void BuildLeftColumn(Control parent)
    {
        var left = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(240, 0),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        parent.AddChild(left);

        left.AddChild(new Label { Text = "Партия" });

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 220),
        };
        left.AddChild(scroll);

        _partyListBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(_partyListBox);

        var addRow = new HBoxContainer();
        _classOption = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _raceOption = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _levelSpin = new SpinBox { MinValue = 1, MaxValue = 60, Value = 3 };
        addRow.AddChild(_classOption);
        addRow.AddChild(_raceOption);
        addRow.AddChild(_levelSpin);
        left.AddChild(addRow);

        var btnRow = new HBoxContainer();
        var addBtn = new Button { Text = "Add", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        addBtn.Pressed += OnAddPressed;
        var clearBtn = new Button { Text = "Clear", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        clearBtn.Pressed += OnClearPressed;
        btnRow.AddChild(addBtn);
        btnRow.AddChild(clearBtn);
        left.AddChild(btnRow);
    }

    // ==================== UI: правая колонка ====================

    private void BuildRightColumn(Control parent)
    {
        var right = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        parent.AddChild(right);

        right.AddChild(new Label { Text = "Квест" });

        var row1 = new HBoxContainer();
        row1.AddChild(new Label { Text = "Шаблон" });
        _templateOption = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _templateOption.ItemSelected += OnTemplateSelected;
        row1.AddChild(_templateOption);
        right.AddChild(row1);

        var row2 = new HBoxContainer();
        row2.AddChild(new Label { Text = "Существо" });
        _creatureOption = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row2.AddChild(_creatureOption);
        right.AddChild(row2);

        var row3 = new HBoxContainer();
        row3.AddChild(new Label { Text = "Code level" });
        _codeLevelSpin = new SpinBox { MinValue = 1, MaxValue = 60, Value = 3 };
        row3.AddChild(_codeLevelSpin);
        right.AddChild(row3);

        right.AddChild(new Label { Text = "Симуляция" });

        var row4 = new HBoxContainer();
        row4.AddChild(new Label { Text = "Trials" });
        _trialsSpin = new SpinBox
        {
            MinValue = 1,
            MaxValue = 1_000_000,
            Value = 1000,
            Step = 100,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        row4.AddChild(_trialsSpin);
        right.AddChild(row4);

        var row5 = new HBoxContainer();
        _randomSeedCheck = new CheckBox { Text = "Random seed", ButtonPressed = true };
        row5.AddChild(_randomSeedCheck);
        row5.AddChild(new Label { Text = "Seed" });
        _seedSpin = new SpinBox
        {
            MinValue = 0,
            MaxValue = int.MaxValue,
            Value = 12345,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        row5.AddChild(_seedSpin);
        right.AddChild(row5);

        var simBtn = new Button { Text = "Simulate" };
        simBtn.Pressed += OnSimulatePressed;
        right.AddChild(simBtn);

        _output = new RichTextLabel
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            ScrollActive = true,
        };
        right.AddChild(_output);
    }

    // ==================== Заполнение выпадающих списков ====================

    private void PopulateClassOptions()
    {
        _classIds.Clear();
        _classOption.Clear();

        foreach (var cls in AdventurerDatabase.Classes.Values.OrderBy(c => c.Id))
        {
            _classIds.Add(cls.Id);
            _classOption.AddItem($"{cls.Id} ({cls.Name.Get("ru")})");
        }
    }

    private void PopulateRaceOptions()
    {
        _raceIds.Clear();
        _raceOption.Clear();

        foreach (var race in AdventurerDatabase.Races.Values.OrderBy(r => r.Id))
        {
            _raceIds.Add(race.Id);
            _raceOption.AddItem($"{race.Id} ({race.Name.Get("ru")})");
        }
    }

    private void PopulateTemplateOptions()
    {
        _templateIds.Clear();
        _templateOption.Clear();

        foreach (var t in QuestDatabase.Templates)
        {
            _templateIds.Add(t.Id);
            _templateOption.AddItem(t.Id);
        }

        if (_templateIds.Count > 0)
        {
            _templateOption.Selected = 0;
            OnTemplateSelected(0);
        }
    }

    private void OnTemplateSelected(long index)
    {
        _creatureIds.Clear();
        _creatureOption.Clear();

        if (index < 0 || index >= _templateIds.Count) return;

        var t = QuestDatabase.Templates.FirstOrDefault(x => x.Id == _templateIds[(int)index]);
        if (t?.Creatures == null) return;

        foreach (var cid in t.Creatures)
        {
            _creatureIds.Add(cid);
            var creature = QuestDatabase.GetCreature(cid);
            string display = creature != null
                ? $"{cid} ({creature.Name.Ru?.Nom})"
                : cid;
            _creatureOption.AddItem(display);
        }

        if (_creatureIds.Count > 0) _creatureOption.Selected = 0;
    }

    // ==================== Обработчики партии ====================

    private void OnAddPressed()
    {
        if (_classIds.Count == 0 || _raceIds.Count == 0) return;

        string classId = _classIds[_classOption.Selected];
        string raceId = _raceIds[_raceOption.Selected];
        int level = (int)_levelSpin.Value;

        var adv = BuildAdventurer(classId, raceId, level);
        if (adv == null) return;

        _party.Add(adv);
        RefreshPartyList();
    }

    private void OnClearPressed()
    {
        _party.Clear();
        RefreshPartyList();
    }

    private void RefreshPartyList()
    {
        foreach (var child in _partyListBox.GetChildren())
            child.QueueFree();

        for (int i = 0; i < _party.Count; i++)
        {
            var a = _party[i];
            var row = new HBoxContainer();
            var lbl = new Label
            {
                Text = $"{a.ClassId} {a.RaceId} L{a.Level.Number}",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            var rm = new Button { Text = "X" };
            int idx = i;
            rm.Pressed += () => { _party.RemoveAt(idx); RefreshPartyList(); };
            row.AddChild(lbl);
            row.AddChild(rm);
            _partyListBox.AddChild(row);
        }
    }

    // ==================== Симуляция ====================

    private void OnSimulatePressed()
    {
        if (_party.Count == 0)
        {
            _output.Text = "Партия пуста.";
            return;
        }

        if (_templateOption.Selected < 0 || _templateIds.Count == 0)
        {
            _output.Text = "Не выбран шаблон.";
            return;
        }

        string templateId = _templateIds[_templateOption.Selected];
        string creatureId = _creatureIds.Count > 0 && _creatureOption.Selected >= 0
            ? _creatureIds[_creatureOption.Selected]
            : null;
        int codeLevel = (int)_codeLevelSpin.Value;
        int trials = (int)_trialsSpin.Value;

        int? seed = _randomSeedCheck.ButtonPressed ? null : (int)_seedSpin.Value;

        var factory = new QuestFactory(seed: 0);
        var quest = factory.CreateSpecific(GameTime.Zero, codeLevel, templateId, creatureId);
        if (quest == null)
        {
            _output.Text = $"Не удалось создать квест '{templateId}' / '{creatureId}'.";
            return;
        }

        var sim = Simulator.Run(quest, _party, trials, seed);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Квест: {quest.Name}  tier={quest.Tier}  lvl={quest.CodeLevel}");
        sb.AppendLine($"Фаз: {quest.Phases.Count}");
        sb.AppendLine($"Партия: {string.Join(", ", _party.Select(a => $"{a.ClassId} {a.RaceId} L{a.Level.Number}"))}");
        sb.AppendLine($"Trials: {sim.Trials}   Seed: {(seed.HasValue ? seed.Value.ToString() : "random")}");
        sb.AppendLine();
        sb.AppendLine($"Triumph : {sim.TriumphChance,6:F2}%   ({sim.TriumphCount})");
        sb.AppendLine($"Success : {sim.SuccessChance,6:F2}%   ({sim.SuccessCount})");
        sb.AppendLine($"Failure : {sim.FailureChance,6:F2}%   ({sim.FailureCount})");
        sb.AppendLine($"Disaster: {sim.DisasterChance,6:F2}%   ({sim.DisasterCount})");
        sb.AppendLine();
        sb.AppendLine($"Success+: {sim.SuccessOrBetterChance,6:F2}%");
        sb.AppendLine($"Avg passed: {sim.AvgPhasesPassed:F2} / {quest.Phases.Count}");
        sb.AppendLine($"Avg exp   : {sim.AvgExpEarned:F1}");

        _output.Text = sb.ToString();
    }

    // ==================== Генерация авантюриста ====================

    private static AdventurerModel BuildAdventurer(string classId, string raceId, int level)
    {
        var cls = AdventurerDatabase.GetClass(classId);
        if (cls == null) return null;

        var ab = AdventurerBalance.Active;
        double referenceValue = ab.StatStartPool + (level - 1) * ab.StatBaseSlope;

        double primaryGrowth = cls.GrowthWeights.TryGetValue(cls.PrimaryStat, out var pg) && pg > 0
            ? pg
            : 1.0;

        var stats = new Dictionary<string, double>();
        foreach (var statId in StatIds.All)
        {
            cls.GrowthWeights.TryGetValue(statId, out var gw);
            stats[statId] = referenceValue * gw / primaryGrowth;
        }

        var race = AdventurerDatabase.GetRace(raceId);
        if (race?.StatBonuses != null)
        {
            foreach (var kv in race.StatBonuses)
                if (stats.ContainsKey(kv.Key))
                    stats[kv.Key] += kv.Value;
        }

        return new AdventurerModel(
            id: Guid.NewGuid().ToString("N"),
            firstName: cls.Name.Get("ru") ?? classId,
            lastName: race?.Name.Get("ru") ?? raceId,
            classId: classId,
            raceId: raceId,
            stats: stats,
            level: level);
    }
}
#endif