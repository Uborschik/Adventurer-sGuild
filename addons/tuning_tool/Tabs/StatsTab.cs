#if TOOLS
using System;
using System.Collections.Generic;
using Godot;

[Tool]
public partial class StatsTab : Control
{
    private const string StatsPath = "res://Resources/Data/Adventurer/AdventurerStats.json";

    private ItemList _list;
    private VBoxContainer _editorRoot;
    private VBoxContainer _skillsBox;
    private Label _status;

    private LineEdit _idEdit, _nameRuEdit, _nameEnEdit;
    private LineEdit _descRuEdit, _descEnEdit;

    private readonly List<AdventurerStatInfo> _stats = new();
    private AdventurerStatInfo _current;

    private sealed class SkillCard
    {
        public LineEdit SkillId;
        public LineEdit NameRu, NameEn;
        public SpinBox StatModifier;
        public VBoxContainer ClassModifiersBox;
        public List<ClassModRow> ClassMods = new();
    }
    private sealed class ClassModRow
    {
        public LineEdit ClassId;
        public SpinBox Modifier;
    }

    private readonly List<SkillCard> _skillCards = new();

    private enum StatusKind { Ok, Warning, Error }

    public override void _Ready()
    {
        GD.Print("[StatsTab] _Ready вызван");
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
        left.AddChild(new Label { Text = "Статы" });

        _list = new ItemList
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 300),
        };
        _list.ItemSelected += OnStatSelected;
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

        _descRuEdit = AddTextRow(_editorRoot, "Desc RU");
        _descEnEdit = AddTextRow(_editorRoot, "Desc EN");

        _editorRoot.AddChild(new HSeparator());
        _editorRoot.AddChild(new Label { Text = "Skills" });
        _editorRoot.AddChild(new Label
        {
            Text = "Навыки, привязанные к этому стату. У каждого — свой модификатор " +
                   "и множители по классам.",
            Modulate = new Color(0.7f, 0.7f, 0.7f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        _skillsBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _editorRoot.AddChild(_skillsBox);
        var addSkillBtn = new Button { Text = "+ skill" };
        addSkillBtn.Pressed += () => AddSkillCard(null);
        _editorRoot.AddChild(addSkillBtn);
    }

    private static LineEdit AddTextRow(Control parent, string label, string tooltip = null)
    {
        var row = new HBoxContainer();
        row.AddChild(new Label
        {
            Text = label,
            CustomMinimumSize = new Vector2(120, 0),
            TooltipText = tooltip ?? "",
        });
        var e = new LineEdit { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(e);
        parent.AddChild(row);
        return e;
    }

    private void RefreshList()
    {
        _list.Clear();
        foreach (var s in _stats)
            _list.AddItem(s.Id);
    }

    private void SelectFirst()
    {
        if (_stats.Count == 0) return;
        _list.Select(0);
        OnStatSelected(0);
    }

    private void OnStatSelected(long index)
    {
        if (index < 0 || index >= _stats.Count) return;
        _current = _stats[(int)index];
        LoadCurrentIntoUI();
    }

    private void LoadCurrentIntoUI()
    {
        var s = _current;
        _idEdit.Text = s.Id ?? "";
        _nameRuEdit.Text = s.Name.Ru ?? "";
        _nameEnEdit.Text = s.Name.En ?? "";
        _descRuEdit.Text = s.Description.Ru ?? "";
        _descEnEdit.Text = s.Description.En ?? "";

        foreach (var child in _skillsBox.GetChildren()) child.QueueFree();
        _skillCards.Clear();

        if (s.Skills != null)
            foreach (var sk in s.Skills)
                AddSkillCard(sk);
    }

    private void AddSkillCard(SkillInfo skill)
    {
        var card = new SkillCard();

        var frame = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var inner = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        frame.AddChild(inner);

        var header = new HBoxContainer();
        header.AddChild(new Label { Text = "skill id", CustomMinimumSize = new Vector2(70, 0) });
        card.SkillId = new LineEdit
        {
            Text = skill?.Id ?? "",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            PlaceholderText = "skill id",
        };
        header.AddChild(card.SkillId);
        var rm = new Button { Text = "X" };
        rm.Pressed += () => { _skillCards.Remove(card); frame.QueueFree(); };
        header.AddChild(rm);
        inner.AddChild(header);

        var nameRow = new HBoxContainer();
        nameRow.AddChild(new Label { Text = "Name RU", CustomMinimumSize = new Vector2(70, 0) });
        card.NameRu = new LineEdit
        {
            Text = skill?.Name.Ru ?? "",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        nameRow.AddChild(card.NameRu);
        nameRow.AddChild(new Label { Text = "EN" });
        card.NameEn = new LineEdit
        {
            Text = skill?.Name.En ?? "",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        nameRow.AddChild(card.NameEn);
        inner.AddChild(nameRow);

        var smRow = new HBoxContainer();
        smRow.AddChild(new Label { Text = "statModifier", CustomMinimumSize = new Vector2(120, 0) });
        card.StatModifier = new SpinBox
        {
            MinValue = 0,
            MaxValue = 10,
            Step = 0.01,
            Value = skill?.StatModifier ?? 1.0,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        smRow.AddChild(card.StatModifier);
        inner.AddChild(smRow);

        inner.AddChild(new Label { Text = "Class modifiers" });
        card.ClassModifiersBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        inner.AddChild(card.ClassModifiersBox);

        var addClassMod = new Button { Text = "+ class modifier" };
        addClassMod.Pressed += () => AddClassModRow(card, null, null);
        inner.AddChild(addClassMod);

        if (skill?.ClassModifiers != null)
            foreach (var kv in skill.ClassModifiers)
                AddClassModRow(card, kv.Key, kv.Value);

        _skillsBox.AddChild(frame);
        _skillCards.Add(card);
    }

    private void AddClassModRow(SkillCard card, string classId, double? modifier)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var classEdit = new LineEdit
        {
            Text = classId ?? "",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            PlaceholderText = "class id",
        };
        var modSpin = new SpinBox
        {
            MinValue = 0,
            MaxValue = 10,
            Step = 0.01,
            Value = modifier ?? 1.0,
            CustomMinimumSize = new Vector2(100, 0),
        };
        var rm = new Button { Text = "X" };
        var cmr = new ClassModRow { ClassId = classEdit, Modifier = modSpin };
        rm.Pressed += () => { card.ClassMods.Remove(cmr); row.QueueFree(); };

        row.AddChild(classEdit);
        row.AddChild(modSpin);
        row.AddChild(rm);
        card.ClassModifiersBox.AddChild(row);
        card.ClassMods.Add(cmr);
    }

    private void OnApplyPressed()
    {
        if (_current == null) { SetStatus("Ничего не выбрано", StatusKind.Error); return; }

        var warnings = SyncUIToCurrent();

        try
        {
            var db = new AdventurerStatDatabase { Stats = _stats };
            JsonWriter.Write(StatsPath, db);
        }
        catch (Exception e)
        {
            SetStatus($"Apply FAILED: {e.Message}", StatusKind.Error);
            GD.PushError($"[StatsTab] Apply: {e}");
            return;
        }

        try { AdventurerDatabase.Load(); QuestDatabase.Load(); }
        catch (Exception e)
        {
            SetStatus($"Записано, но Load упал: {e.Message}", StatusKind.Error);
            GD.PushError($"[StatsTab] Load after apply: {e}");
            return;
        }

        if (warnings.Count > 0)
        {
            SetStatus($"Applied ({_stats.Count}). Предупреждения: {string.Join("; ", warnings)}",
                      StatusKind.Warning);
        }
        else
        {
            SetStatus($"Applied: {_stats.Count} статов записано", StatusKind.Ok);
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
            SetStatus($"Reverted: {_stats.Count} статов перезагружено", StatusKind.Ok);
        }
        catch (Exception e)
        {
            SetStatus($"Revert: Load упал: {e.Message}", StatusKind.Error);
        }
    }

    private List<string> SyncUIToCurrent()
    {
        var warnings = new List<string>();
        var s = _current;

        s.Name = new LocalizedString { Ru = _nameRuEdit.Text, En = _nameEnEdit.Text };
        s.Description = new LocalizedString { Ru = _descRuEdit.Text, En = _descEnEdit.Text };

        var skills = new List<SkillInfo>();
        foreach (var card in _skillCards)
        {
            string sid = card.SkillId.Text?.Trim();
            if (string.IsNullOrEmpty(sid)) continue;

            var skill = new SkillInfo
            {
                Id = sid,
                Name = new LocalizedString { Ru = card.NameRu.Text, En = card.NameEn.Text },
                StatModifier = NumericHelpers.Round4(card.StatModifier.Value),
                ClassModifiers = new Dictionary<string, double>(),
            };

            var seen = new HashSet<string>();
            foreach (var cm in card.ClassMods)
            {
                string cid = cm.ClassId.Text?.Trim();
                if (string.IsNullOrEmpty(cid)) continue;
                if (!seen.Add(cid))
                    warnings.Add($"skill '{sid}': дубликат classModifier '{cid}'");
                skill.ClassModifiers[cid] = NumericHelpers.Round4(cm.Modifier.Value);
            }

            skills.Add(skill);
        }
        s.Skills = skills;

        return warnings;
    }

    private void ReloadFromDisk()
    {
        try
        {
            var db = JsonLoader.Load<AdventurerStatDatabase>(StatsPath);
            _stats.Clear();
            if (db?.Stats != null) _stats.AddRange(db.Stats);
            GD.Print($"[StatsTab] ReloadFromDisk: загружено {_stats.Count} статов");
        }
        catch (Exception e)
        {
            GD.PushError($"[StatsTab] Не удалось загрузить {StatsPath}: {e}");
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