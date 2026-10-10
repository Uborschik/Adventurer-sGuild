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
using Godot;

[Tool]
public partial class StatsTab : Control
{
    private const string StatsPath = "res://Resources/Data/Adventurer/AdventurerStats.json";

    private const float LabelW = 130;
    private const float FormWidth = 750;

    private ItemList _list;
    private VBoxContainer _editorRoot;
    private Label _status;

    private LineEdit _idEdit;
    private LineEdit _nameRuEdit, _nameEnEdit;
    private LineEdit _descRuEdit, _descEnEdit;

    private VBoxContainer _skillsBox;
    private readonly List<SkillCard> _skillCards = new();

    private readonly List<AdventurerStatInfo> _stats = new();
    private AdventurerStatInfo _current;

    private enum StatusKind { Ok, Warning, Error }

    private sealed class SkillCard
    {
        public OptionButton SkillId;
        public LineEdit NameRu, NameEn;
        public SpinBox StatModifier;
        public Dictionary<string, SpinBox> ClassModifierBoxes = new();
    }

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

    // ==================== Левая колонка ====================

    private void BuildLeft(Control parent)
    {
        var left = new VBoxContainer { CustomMinimumSize = new Vector2(200, 0) };
        left.AddThemeConstantOverride("separation", 4);
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

        var (_, skillsBox) = CollapsibleSection(_editorRoot, "Skills", expanded: true);
        BuildSkillsSection(skillsBox);
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

        var descRuRow = FormRow(parent);
        descRuRow.AddChild(FormLabel("Desc RU"));
        _descRuEdit = FormField();
        descRuRow.AddChild(_descRuEdit);

        var descEnRow = FormRow(parent);
        descEnRow.AddChild(FormLabel("Desc EN"));
        _descEnEdit = FormField();
        descEnRow.AddChild(_descEnEdit);
    }

    private void BuildSkillsSection(Control parent)
    {
        parent.AddChild(new Label
        {
            Text = "Навыки, привязанные к этому стату. statModifier и множители по классам.",
            Modulate = new Color(0.7f, 0.7f, 0.7f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        _skillsBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _skillsBox.AddThemeConstantOverride("separation", 8);
        parent.AddChild(_skillsBox);

        var addSkillBtn = new Button
        {
            Text = "+ skill",
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        addSkillBtn.Pressed += () => AddSkillCard(null);
        parent.AddChild(addSkillBtn);
    }

    private void AddSkillCard(SkillInfo skill)
    {
        var card = new SkillCard();

        var frame = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var inner = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        inner.AddThemeConstantOverride("separation", 6);
        frame.AddChild(inner);

        // Row 1: skill id (dropdown из SkillIds.All) + ×
        var idRow = FormRow(inner);
        idRow.AddChild(FormLabel("skill id"));
        card.SkillId = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        PopulateSkillIdOptions(card.SkillId, skill?.Id);
        idRow.AddChild(card.SkillId);

        var rm = new Button
        {
            Text = "×",
            Flat = true,
            CustomMinimumSize = new Vector2(28, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        rm.Pressed += () => { _skillCards.Remove(card); frame.QueueFree(); };
        idRow.AddChild(rm);

        // Row 2: names
        var nameRow = FormRow(inner);
        nameRow.AddChild(FormLabel("Name RU"));
        card.NameRu = FormField();
        card.NameRu.Text = skill?.Name.Ru ?? "";
        nameRow.AddChild(card.NameRu);
        nameRow.AddChild(FormLabel("Name EN"));
        card.NameEn = FormField();
        card.NameEn.Text = skill?.Name.En ?? "";
        nameRow.AddChild(card.NameEn);

        // Row 3: statModifier
        var smRow = FormRow(inner);
        smRow.AddChild(FormLabel("statModifier"));
        card.StatModifier = new SpinBox
        {
            MinValue = 0,
            MaxValue = 10,
            Step = 0.01,
            Value = skill?.StatModifier ?? 1.0,
            CustomMinimumSize = new Vector2(120, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        smRow.AddChild(card.StatModifier);
        smRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        // Class modifiers — фиксированная сетка 4×2 по всем классам базы.
        inner.AddChild(new Label
        {
            Text = "Class modifiers",
            Modulate = new Color(0.85f, 0.85f, 0.85f),
        });

        var grid = new GridContainer { Columns = 4 };
        grid.AddThemeConstantOverride("h_separation", 16);
        grid.AddThemeConstantOverride("v_separation", 4);
        inner.AddChild(grid);

        card.ClassModifierBoxes.Clear();

        foreach (var cls in AdventurerDatabase.Classes.Values.OrderBy(c => c.Id))
        {
            var cell = new HBoxContainer();
            cell.AddThemeConstantOverride("separation", 6);
            cell.AddChild(new Label
            {
                Text = cls.Id,
                CustomMinimumSize = new Vector2(72, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Modulate = new Color(0.85f, 0.85f, 0.85f),
            });

            var spin = new SpinBox
            {
                MinValue = 0,
                MaxValue = 10,
                Step = 0.01,
                Value = 1.0,
                CustomMinimumSize = new Vector2(80, 0),
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            };

            if (skill?.ClassModifiers != null
                && skill.ClassModifiers.TryGetValue(cls.Id, out var v))
                spin.Value = v;

            cell.AddChild(spin);
            grid.AddChild(cell);

            card.ClassModifierBoxes[cls.Id] = spin;
        }

        _skillsBox.AddChild(frame);
        _skillCards.Add(card);
    }

    // ==================== Skill id dropdown helpers ====================

    private static void PopulateSkillIdOptions(OptionButton option, string currentId)
    {
        option.Clear();

        foreach (var sid in SkillIds.All)
        {
            string name = AdventurerDatabase.GetSkill(sid)?.Name.Get(Loc.Language) ?? "";
            option.AddItem(string.IsNullOrEmpty(name) ? sid : $"{sid}  ({name})");
        }

        // Если текущий skill id не из SkillIds.All — добавляем его как «устаревший»,
        // чтобы пользователь видел невалидное значение и мог его заменить.
        if (!string.IsNullOrEmpty(currentId)
            && !SkillIds.All.Contains(currentId))
        {
            option.AddItem($"{currentId}  (неизвестный)");
            option.Selected = option.ItemCount - 1;
            return;
        }

        // Выбираем текущий, если он есть в списке.
        for (int i = 0; i < option.ItemCount; i++)
        {
            string text = option.GetItemText(i);
            int spaceIdx = text.IndexOf("  (", StringComparison.Ordinal);
            string id = spaceIdx > 0 ? text.Substring(0, spaceIdx) : text;
            if (id == currentId)
            {
                option.Selected = i;
                return;
            }
        }

        // Фолбэк: выбираем первый доступный.
        if (option.ItemCount > 0) option.Selected = 0;
    }

    private static string ReadSkillId(OptionButton option)
    {
        if (option.Selected < 0 || option.Selected >= option.ItemCount) return null;
        string text = option.GetItemText(option.Selected);
        int spaceIdx = text.IndexOf("  (", StringComparison.Ordinal);
        return spaceIdx > 0 ? text.Substring(0, spaceIdx) : text;
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

    // ==================== Список ====================

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

        foreach (var child in _skillsBox.GetChildren())
            child.QueueFree();
        _skillCards.Clear();

        var warnings = new List<string>();

        if (s.Skills != null)
        {
            foreach (var sk in s.Skills)
            {
                if (sk == null || string.IsNullOrEmpty(sk.Id))
                {
                    warnings.Add("пустой skill id");
                    continue;
                }
                AddSkillCard(sk);
            }
        }

        if (s.Skills == null || s.Skills.Count == 0)
            warnings.Add("нет навыков — стат никогда не будет валидным");

        SetStatus(warnings.Count > 0 ? string.Join("; ", warnings) : "",
                  warnings.Count > 0 ? StatusKind.Warning : StatusKind.Ok);
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
                var dict = new Dictionary<string, AdventurerStatInfo>();
                foreach (var s in _stats) dict[s.Id] = s;
                AdventurerDatabase.Install(stats: dict);

                string suffix = warnings.Count > 0
                    ? $" Предупреждения: {string.Join("; ", warnings)}"
                    : "";
                SetStatus($"Preview: {_stats.Count} статов в памяти (диск не тронут).{suffix}",
                          StatusKind.Warning);
            }
            catch (Exception e)
            {
                SetStatus($"Preview FAILED: {e.Message}", StatusKind.Error);
                GD.PushError($"[StatsTab] Preview: {e}");
            }
            return;
        }

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

        SetStatus(warnings.Count > 0
            ? $"Applied ({_stats.Count}). Предупреждения: {string.Join("; ", warnings)}"
            : $"Applied: {_stats.Count} статов записано",
            warnings.Count > 0 ? StatusKind.Warning : StatusKind.Ok);
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

        // Собираем skill id из ДРУГИХ статов, чтобы предупредить о переносах.
        var takenByOtherStats = new HashSet<string>();
        foreach (var otherStat in _stats)
        {
            if (ReferenceEquals(otherStat, s)) continue;
            if (otherStat.Skills == null) continue;
            foreach (var sk in otherStat.Skills)
                if (sk?.Id != null) takenByOtherStats.Add(sk.Id);
        }

        var skills = new List<SkillInfo>();
        var seenSkillIds = new HashSet<string>();

        foreach (var card in _skillCards)
        {
            string sid = ReadSkillId(card.SkillId);
            if (string.IsNullOrEmpty(sid)) continue;

            if (!seenSkillIds.Add(sid))
                warnings.Add($"дубликат скилла '{sid}' в этом стате");

            if (takenByOtherStats.Contains(sid))
                warnings.Add($"skill '{sid}' уже используется в другом стате");

            var skill = new SkillInfo
            {
                Id = sid,
                Name = new LocalizedString
                {
                    Ru = card.NameRu.Text,
                    En = card.NameEn.Text,
                },
                StatModifier = NumericHelpers.Round4(card.StatModifier.Value),
                ClassModifiers = new Dictionary<string, double>(),
            };

            foreach (var kv in card.ClassModifierBoxes)
                skill.ClassModifiers[kv.Key] = NumericHelpers.Round4(kv.Value.Value);

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