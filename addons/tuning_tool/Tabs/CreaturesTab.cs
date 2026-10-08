#if TOOLS
using System;
using System.Collections.Generic;
using Godot;

[Tool]
public partial class CreaturesTab : Control
{
    private const string CreaturesPath = "res://Resources/Data/Quest/Creatures.json";

    private ItemList _list;
    private VBoxContainer _editorRoot;
    private Label _status;

    private LineEdit _idEdit;
    private OptionButton _tierOption;
    private LineEdit _nameEnEdit;
    private LineEdit _nomEdit, _genEdit, _datEdit, _accEdit, _insEdit, _preEdit;

    private VBoxContainer _modifiesBox;
    private VBoxContainer _addsBox;

    private readonly List<CreatureInfo> _creatures = new();
    private CreatureInfo _current;

    private sealed class ModifierCard
    {
        public LineEdit Phase;
        public CheckBox HasDcDelta;
        public SpinBox DcDelta;
        public CheckBox HasDuration;
        public SpinBox Duration;
        public VBoxContainer StatsBox;
        public List<StatRow> Stats = new();
    }

    private sealed class StatRow
    {
        public LineEdit Stat;
        public SpinBox DcDelta;
    }

    private sealed class AddedPhaseCard
    {
        public LineEdit Phase;
        public LineEdit Before;
        public VBoxContainer SolutionsBox;
        public List<SolutionCard> Solutions = new();
    }

    private sealed class SolutionCard
    {
        public List<SkillRow> Skills = new();
    }

    private sealed class SkillRow
    {
        public LineEdit Skill;
        public SpinBox Dc;
    }

    private readonly List<ModifierCard> _modifierCards = new();
    private readonly List<AddedPhaseCard> _addedPhaseCards = new();

    private bool _suppressSignals;

    private static readonly string[] TierOptions = { "easy", "normal", "hard" };

    private enum StatusKind { Ok, Warning, Error }

    public override void _Ready()
    {
        GD.Print("[CreaturesTab] _Ready вызван");
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
        left.AddChild(new Label { Text = "Существа" });

        _list = new ItemList
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 300),
        };
        _list.ItemSelected += OnCreatureSelected;
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

        _idEdit = AddTextRow(_editorRoot, "Id");
        _idEdit.Editable = false;

        var tierRow = new HBoxContainer();
        tierRow.AddChild(new Label { Text = "Tier", CustomMinimumSize = new Vector2(120, 0) });
        _tierOption = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (var t in TierOptions) _tierOption.AddItem(t);
        tierRow.AddChild(_tierOption);
        _editorRoot.AddChild(tierRow);

        _editorRoot.AddChild(new HSeparator());
        _editorRoot.AddChild(new Label { Text = "Name RU" });
        _nomEdit = AddTextRow(_editorRoot, "nom");
        _genEdit = AddTextRow(_editorRoot, "gen");
        _datEdit = AddTextRow(_editorRoot, "dat");
        _accEdit = AddTextRow(_editorRoot, "acc");
        _insEdit = AddTextRow(_editorRoot, "ins");
        _preEdit = AddTextRow(_editorRoot, "pre");

        _editorRoot.AddChild(new Label { Text = "Name EN" });
        _nameEnEdit = AddTextRow(_editorRoot, "en");

        _editorRoot.AddChild(new HSeparator());
        _editorRoot.AddChild(new Label { Text = "Modifies" });
        _modifiesBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _editorRoot.AddChild(_modifiesBox);
        var addModBtn = new Button { Text = "+ phase" };
        addModBtn.Pressed += () => AddModifierCard(null, null);
        _editorRoot.AddChild(addModBtn);

        _editorRoot.AddChild(new HSeparator());
        _editorRoot.AddChild(new Label { Text = "Adds" });
        _addsBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _editorRoot.AddChild(_addsBox);
        var addAddedBtn = new Button { Text = "+ added phase" };
        addAddedBtn.Pressed += () => AddAddedPhaseCard(null);
        _editorRoot.AddChild(addAddedBtn);
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

    // ==================== Список ====================

    private void RefreshList()
    {
        _suppressSignals = true;
        _list.Clear();
        for (int i = 0; i < _creatures.Count; i++)
            _list.AddItem(_creatures[i].Id);
        _suppressSignals = false;
    }

    private void SelectFirst()
    {
        if (_creatures.Count == 0) return;
        _list.Select(0);
        OnCreatureSelected(0);
    }

    private void OnCreatureSelected(long index)
    {
        if (_suppressSignals) return;
        if (index < 0 || index >= _creatures.Count) return;
        _current = _creatures[(int)index];
        LoadCurrentIntoUI();
    }

    private void LoadCurrentIntoUI()
    {
        _suppressSignals = true;
        var c = _current;

        _idEdit.Text = c.Id ?? "";
        int tierIdx = Array.IndexOf(TierOptions, c.Tier);
        _tierOption.Selected = tierIdx >= 0 ? tierIdx : 1;

        var n = c.Name;
        _nomEdit.Text = n?.Ru?.Nom ?? "";
        _genEdit.Text = n?.Ru?.Gen ?? "";
        _datEdit.Text = n?.Ru?.Dat ?? "";
        _accEdit.Text = n?.Ru?.Acc ?? "";
        _insEdit.Text = n?.Ru?.Ins ?? "";
        _preEdit.Text = n?.Ru?.Pre ?? "";
        _nameEnEdit.Text = n?.En ?? "";

        foreach (var child in _modifiesBox.GetChildren()) child.QueueFree();
        _modifierCards.Clear();
        if (c.PhaseEffects?.Modifies != null)
            foreach (var kv in c.PhaseEffects.Modifies)
                AddModifierCard(kv.Key, kv.Value);

        foreach (var child in _addsBox.GetChildren()) child.QueueFree();
        _addedPhaseCards.Clear();
        if (c.PhaseEffects?.Adds != null)
            foreach (var a in c.PhaseEffects.Adds)
                AddAddedPhaseCard(a);

        _suppressSignals = false;
    }

    // ==================== Modifier card ====================

    private void AddModifierCard(string phaseId, PhaseModifier mod)
    {
        var card = new ModifierCard();

        var frame = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var inner = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        frame.AddChild(inner);

        var header = new HBoxContainer();
        header.AddChild(new Label { Text = "phase", CustomMinimumSize = new Vector2(60, 0) });
        card.Phase = new LineEdit
        {
            Text = phaseId ?? "",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            PlaceholderText = "phase id",
        };
        header.AddChild(card.Phase);
        var rm = new Button { Text = "X" };
        rm.Pressed += () => { _modifierCards.Remove(card); frame.QueueFree(); };
        header.AddChild(rm);
        inner.AddChild(header);

        var dcRow = new HBoxContainer();
        card.HasDcDelta = new CheckBox { ButtonPressed = mod?.DcDelta.HasValue ?? false };
        card.DcDelta = new SpinBox
        {
            MinValue = -5,
            MaxValue = 5,
            Step = 0.05,
            Value = mod?.DcDelta ?? 0,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        card.DcDelta.Editable = card.HasDcDelta.ButtonPressed;
        card.HasDcDelta.Toggled += v => card.DcDelta.Editable = v;
        dcRow.AddChild(new Label { Text = "DcDelta", CustomMinimumSize = new Vector2(120, 0) });
        dcRow.AddChild(card.HasDcDelta);
        dcRow.AddChild(card.DcDelta);
        inner.AddChild(dcRow);

        var durRow = new HBoxContainer();
        card.HasDuration = new CheckBox { ButtonPressed = mod?.DurationDeltaMinutes.HasValue ?? false };
        card.Duration = new SpinBox
        {
            MinValue = -10000,
            MaxValue = 10000,
            Step = 1,
            Value = mod?.DurationDeltaMinutes ?? 0,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        card.Duration.Editable = card.HasDuration.ButtonPressed;
        card.HasDuration.Toggled += v => card.Duration.Editable = v;
        durRow.AddChild(new Label { Text = "DurationΔ", CustomMinimumSize = new Vector2(120, 0) });
        durRow.AddChild(card.HasDuration);
        durRow.AddChild(card.Duration);
        inner.AddChild(durRow);

        inner.AddChild(new Label { Text = "Stat modifiers" });
        card.StatsBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        inner.AddChild(card.StatsBox);

        var addStat = new Button { Text = "+ stat" };
        addStat.Pressed += () => AddStatRow(card, null, null);
        inner.AddChild(addStat);

        if (mod?.StatModifiers != null)
            foreach (var kv in mod.StatModifiers)
                AddStatRow(card, kv.Key, kv.Value.DcDelta);

        _modifiesBox.AddChild(frame);
        _modifierCards.Add(card);
    }

    private void AddStatRow(ModifierCard card, string statId, double? dcDelta)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var statEdit = new LineEdit
        {
            Text = statId ?? "",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            PlaceholderText = "stat id",
        };
        var dcSpin = new SpinBox
        {
            MinValue = -5,
            MaxValue = 5,
            Step = 0.05,
            Value = dcDelta ?? 0,
            CustomMinimumSize = new Vector2(90, 0),
        };
        var rm = new Button { Text = "X" };
        var sr = new StatRow { Stat = statEdit, DcDelta = dcSpin };
        rm.Pressed += () => { card.Stats.Remove(sr); row.QueueFree(); };

        row.AddChild(statEdit);
        row.AddChild(dcSpin);
        row.AddChild(rm);
        card.StatsBox.AddChild(row);
        card.Stats.Add(sr);
    }

    // ==================== Added phase card ====================

    private void AddAddedPhaseCard(AddedPhase ap)
    {
        var card = new AddedPhaseCard();

        var frame = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var inner = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        frame.AddChild(inner);

        var header = new HBoxContainer();
        header.AddChild(new Label { Text = "phase", CustomMinimumSize = new Vector2(60, 0) });
        card.Phase = new LineEdit { Text = ap?.Phase ?? "", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        header.AddChild(card.Phase);
        header.AddChild(new Label { Text = "before" });
        card.Before = new LineEdit { Text = ap?.Before ?? "", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        header.AddChild(card.Before);
        var rm = new Button { Text = "X" };
        rm.Pressed += () => { _addedPhaseCards.Remove(card); frame.QueueFree(); };
        header.AddChild(rm);
        inner.AddChild(header);

        inner.AddChild(new Label { Text = "Solutions" });
        card.SolutionsBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        inner.AddChild(card.SolutionsBox);

        var addSol = new Button { Text = "+ solution" };
        addSol.Pressed += () => AddSolutionCard(card, null);
        inner.AddChild(addSol);

        if (ap?.Solutions != null)
            foreach (var s in ap.Solutions)
                AddSolutionCard(card, s);

        _addsBox.AddChild(frame);
        _addedPhaseCards.Add(card);
    }

    private void AddSolutionCard(AddedPhaseCard apCard, PhaseSolution sol)
    {
        var sc = new SolutionCard();
        var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var header = new HBoxContainer();
        header.AddChild(new Label { Text = "Solution", SizeFlagsHorizontal = SizeFlags.ExpandFill });
        var rm = new Button { Text = "X" };
        rm.Pressed += () => { apCard.Solutions.Remove(sc); root.QueueFree(); };
        header.AddChild(rm);
        root.AddChild(header);

        var skillsBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        root.AddChild(skillsBox);

        var addSkill = new Button { Text = "+ skill" };
        addSkill.Pressed += () => AddSkillRow(sc, skillsBox, null, 0);
        root.AddChild(addSkill);

        apCard.SolutionsBox.AddChild(root);
        apCard.Solutions.Add(sc);

        if (sol?.Skills != null)
            foreach (var kv in sol.Skills)
                AddSkillRow(sc, skillsBox, kv.Key, kv.Value);
    }

    private void AddSkillRow(SolutionCard sc, VBoxContainer parent, string skillId, double dc)
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
        var skr = new SkillRow { Skill = skillEdit, Dc = dcSpin };
        rm.Pressed += () => { sc.Skills.Remove(skr); row.QueueFree(); };

        row.AddChild(skillEdit);
        row.AddChild(dcSpin);
        row.AddChild(rm);
        parent.AddChild(row);
        sc.Skills.Add(skr);
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
                var dict = new Dictionary<string, CreatureInfo>();
                foreach (var c in _creatures) dict[c.Id] = c;
                QuestDatabase.ApplyData(creatures: dict);

                string suffix = warnings.Count > 0
                    ? $" Предупреждения: {string.Join("; ", warnings)}"
                    : "";
                SetStatus($"Preview: {_creatures.Count} существ в памяти (диск не тронут).{suffix}",
                          warnings.Count > 0 ? StatusKind.Warning : StatusKind.Warning);
            }
            catch (Exception e)
            {
                SetStatus($"Preview FAILED: {e.Message}", StatusKind.Error);
                GD.PushError($"[CreaturesTab] Preview: {e}");
            }
            return;
        }

        try
        {
            var db = new CreatureDatabase { Creatures = _creatures };
            JsonWriter.Write(CreaturesPath, db);
        }
        catch (Exception e)
        {
            SetStatus($"Apply FAILED: {e.Message}", StatusKind.Error);
            GD.PushError($"[CreaturesTab] Apply: {e}");
            return;
        }

        try { QuestDatabase.Load(); }
        catch (Exception e)
        {
            SetStatus($"Записано, но QuestDatabase.Load упал: {e.Message}", StatusKind.Error);
            GD.PushError($"[CreaturesTab] QuestDatabase.Load: {e}");
            return;
        }

        if (warnings.Count > 0)
        {
            string msg = $"Applied ({_creatures.Count} существ). Предупреждения: " +
                         string.Join("; ", warnings);
            SetStatus(msg, StatusKind.Warning);
            GD.PushWarning($"[CreaturesTab] {msg}");
        }
        else
        {
            SetStatus($"Applied: {_creatures.Count} существ записано в JSON", StatusKind.Ok);
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
            SetStatus($"Reverted: {_creatures.Count} существ перезагружено", StatusKind.Ok);
        }
        catch (Exception e)
        {
            SetStatus($"Revert: QuestDatabase.Load упал: {e.Message}", StatusKind.Error);
        }
    }

    // ==================== Sync UI -> модель ====================

    // Возвращает список предупреждений (дубликаты ключей, пустые id и т.п.)
    private List<string> SyncUIToCurrent()
    {
        var warnings = new List<string>();
        var c = _current;

        // Name
        if (c.Name == null) c.Name = new LocalizedNoun();
        if (c.Name.Ru == null) c.Name.Ru = new NounForms();

        c.Name.Ru.Nom = EmptyToNull(_nomEdit.Text);
        c.Name.Ru.Gen = EmptyToNull(_genEdit.Text);
        c.Name.Ru.Dat = EmptyToNull(_datEdit.Text);
        c.Name.Ru.Acc = EmptyToNull(_accEdit.Text);
        c.Name.Ru.Ins = EmptyToNull(_insEdit.Text);
        c.Name.Ru.Pre = EmptyToNull(_preEdit.Text);
        c.Name.En = _nameEnEdit.Text;

        if (_tierOption.Selected >= 0 && _tierOption.Selected < TierOptions.Length)
            c.Tier = TierOptions[_tierOption.Selected];

        if (c.PhaseEffects == null) c.PhaseEffects = new CreaturePhaseEffects();

        // Modifies
        var mods = new Dictionary<string, PhaseModifier>();
        var seenModPhases = new HashSet<string>();
        foreach (var card in _modifierCards)
        {
            string phaseId = card.Phase.Text?.Trim();
            if (string.IsNullOrEmpty(phaseId)) continue;

            if (!seenModPhases.Add(phaseId))
                warnings.Add($"Modifies: дубликат фазы '{phaseId}', используется последняя карточка");

            var mod = new PhaseModifier();
            if (card.HasDcDelta.ButtonPressed) mod.DcDelta = card.DcDelta.Value;
            if (card.HasDuration.ButtonPressed) mod.DurationDeltaMinutes = (int)card.Duration.Value;

            if (card.Stats.Count > 0)
            {
                var stats = new Dictionary<string, StatDcDelta>();
                var seenStats = new HashSet<string>();
                foreach (var s in card.Stats)
                {
                    string sid = s.Stat.Text?.Trim();
                    if (string.IsNullOrEmpty(sid)) continue;

                    if (!seenStats.Add(sid))
                        warnings.Add($"Modifies['{phaseId}']: дубликат стата '{sid}'");

                    stats[sid] = new StatDcDelta { DcDelta = NumericHelpers.Round4(s.DcDelta.Value) };
                }
                if (stats.Count > 0) mod.StatModifiers = stats;
            }

            mods[phaseId] = mod;
        }
        c.PhaseEffects.Modifies = mods.Count > 0 ? mods : null;

        // Adds
        var adds = new List<AddedPhase>();
        foreach (var apCard in _addedPhaseCards)
        {
            string phaseId = apCard.Phase.Text?.Trim();
            if (string.IsNullOrEmpty(phaseId)) continue;

            var ap = new AddedPhase
            {
                Phase = phaseId,
                Before = EmptyToNull(apCard.Before.Text),
                Solutions = new List<PhaseSolution>(),
            };

            for (int si = 0; si < apCard.Solutions.Count; si++)
            {
                var sc = apCard.Solutions[si];
                var skills = new Dictionary<string, double>();
                var seenSkills = new HashSet<string>();
                foreach (var s in sc.Skills)
                {
                    string sid = s.Skill.Text?.Trim();
                    if (string.IsNullOrEmpty(sid)) continue;

                    if (!seenSkills.Add(sid))
                        warnings.Add($"Adds['{phaseId}'].sol[{si}]: дубликат скилла '{sid}'");

                    skills[sid] = s.Dc.Value;
                }
                ap.Solutions.Add(new PhaseSolution { Skills = skills });
            }

            adds.Add(ap);
        }
        c.PhaseEffects.Adds = adds.Count > 0 ? adds : null;

        return warnings;
    }

    private static string EmptyToNull(string s)
        => string.IsNullOrEmpty(s) ? null : s;

    // ==================== Прочее ====================

    private void ReloadFromDisk()
    {
        try
        {
            var db = JsonLoader.Load<CreatureDatabase>(CreaturesPath);
            _creatures.Clear();
            if (db?.Creatures != null) _creatures.AddRange(db.Creatures);
            GD.Print($"[CreaturesTab] ReloadFromDisk: загружено {_creatures.Count} существ");
        }
        catch (Exception e)
        {
            GD.PushError($"[CreaturesTab] Не удалось загрузить {CreaturesPath}: {e}");
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