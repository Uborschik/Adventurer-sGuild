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
public partial class ClassesTab : Control
{
    private const string ClassesPath = "res://Resources/Data/Adventurer/AdventurerClasses.json";

    private const float LabelW = 130;
    private const float FormWidth = 750;

    private ItemList _list;
    private VBoxContainer _editorRoot;
    private Label _status;

    private LineEdit _idEdit;
    private LineEdit _nameRuEdit, _nameEnEdit;

    private OptionButton _primaryStatOption, _primarySkillOption;
    private OptionButton _secondaryStatOption, _secondarySkillOption;
    private SpinBox _weightSpin;

    private readonly List<string> _primarySkillIds = new();
    private readonly List<string> _secondarySkillIds = new();

    private readonly Dictionary<string, SpinBox> _growthBoxes = new();

    private readonly List<AdventurerClassInfo> _classes = new();
    private AdventurerClassInfo _current;

    private enum StatusKind { Ok, Warning, Error }

    public override void _Ready()
    {
        GD.Print("[ClassesTab] _Ready вызван");
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
        left.AddChild(new Label { Text = "Классы" });

        _list = new ItemList
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 300),
        };
        _list.ItemSelected += OnClassSelected;
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

        _editorRoot = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _editorRoot.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(_editorRoot);

        var (_, identityBox) = CollapsibleSection(_editorRoot, "Identity", expanded: true);
        BuildIdentitySection(identityBox);

        var (_, coreBox) = CollapsibleSection(_editorRoot, "Core", expanded: true);
        BuildCoreSection(coreBox);

        var (_, growthBox) = CollapsibleSection(_editorRoot, "Growth weights", expanded: true);
        BuildGrowthSection(growthBox);
    }

    private void BuildIdentitySection(Control parent)
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
    }

    private void BuildCoreSection(Control parent)
    {
        var r1 = FormRow(parent);
        r1.AddChild(FormLabel("Primary stat"));
        _primaryStatOption = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _primaryStatOption.ItemSelected += _ => OnPrimaryStatChanged();
        r1.AddChild(_primaryStatOption);

        r1.AddChild(FormLabel("Primary skill"));
        _primarySkillOption = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        r1.AddChild(_primarySkillOption);

        var r2 = FormRow(parent);
        r2.AddChild(FormLabel("Secondary stat"));
        _secondaryStatOption = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _secondaryStatOption.ItemSelected += _ => OnSecondaryStatChanged();
        r2.AddChild(_secondaryStatOption);

        r2.AddChild(FormLabel("Secondary skill"));
        _secondarySkillOption = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        r2.AddChild(_secondarySkillOption);

        var r3 = FormRow(parent);
        r3.AddChild(FormLabel("Weight"));
        _weightSpin = new SpinBox
        {
            MinValue = 0,
            MaxValue = 1000,
            Step = 1,
            Value = 0,
            CustomMinimumSize = new Vector2(120, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        r3.AddChild(_weightSpin);
        r3.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        PopulateStatOptions(_primaryStatOption);
        PopulateStatOptions(_secondaryStatOption);
    }

    private void BuildGrowthSection(Control parent)
    {
        parent.AddChild(new Label
        {
            Text = "Веса роста статов. Primary stat обычно = 1.00, остальные меньше.",
            Modulate = new Color(0.7f, 0.7f, 0.7f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        var grid = new GridContainer { Columns = 3 };
        grid.AddThemeConstantOverride("h_separation", 16);
        grid.AddThemeConstantOverride("v_separation", 4);
        parent.AddChild(grid);

        _growthBoxes.Clear();

        foreach (var statId in StatIds.All)
        {
            var cell = new HBoxContainer();
            cell.AddThemeConstantOverride("separation", 6);
            cell.AddChild(new Label
            {
                Text = statId,
                CustomMinimumSize = new Vector2(32, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Modulate = new Color(0.85f, 0.85f, 0.85f),
            });

            var spin = new SpinBox
            {
                MinValue = 0,
                MaxValue = 10,
                Step = 0.01,
                Value = 0,
                CustomMinimumSize = new Vector2(80, 0),
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            };
            cell.AddChild(spin);
            _growthBoxes[statId] = spin;
            grid.AddChild(cell);
        }
    }

    // ==================== Дропдауны статов/скиллов ====================

    private static void PopulateStatOptions(OptionButton option)
    {
        option.Clear();
        foreach (var statId in StatIds.All)
        {
            if (!AdventurerDatabase.Stats.ContainsKey(statId)) continue;
            string name = AdventurerDatabase.StatName(statId);
            option.AddItem(string.IsNullOrEmpty(name) ? statId : $"{statId}  ({name})");
        }
    }

    private static string ReadStatId(OptionButton option)
    {
        if (option.Selected < 0 || option.Selected >= option.ItemCount) return null;
        string text = option.GetItemText(option.Selected);
        int spaceIdx = text.IndexOf("  (", StringComparison.Ordinal);
        return spaceIdx > 0 ? text.Substring(0, spaceIdx) : text;
    }

    private static int FindStatIndex(OptionButton option, string statId)
    {
        if (string.IsNullOrEmpty(statId)) return -1;
        for (int i = 0; i < option.ItemCount; i++)
        {
            string text = option.GetItemText(i);
            int spaceIdx = text.IndexOf("  (", StringComparison.Ordinal);
            string id = spaceIdx > 0 ? text.Substring(0, spaceIdx) : text;
            if (id == statId) return i;
        }
        return -1;
    }

    /// <summary>
    /// Пересобирает список скиллов выбранного стата.
    /// Если wantSkillId != null и присутствует среди скиллов — выставляем его выбранным.
    /// Иначе выбираем первый доступный.
    /// </summary>
    private static void PopulateSkillOptions(
        OptionButton skillOption, List<string> skillIds,
        string statId, string wantSkillId)
    {
        skillOption.Clear();
        skillIds.Clear();

        if (string.IsNullOrEmpty(statId)
            || !AdventurerDatabase.Stats.TryGetValue(statId, out var statInfo)
            || statInfo.Skills == null)
        {
            return;
        }

        int selectIdx = -1;
        int i = 0;
        foreach (var skill in statInfo.Skills)
        {
            if (skill == null || string.IsNullOrEmpty(skill.Id)) continue;

            skillIds.Add(skill.Id);
            string name = skill.Name.Get(Loc.Language);
            skillOption.AddItem(string.IsNullOrEmpty(name)
                ? skill.Id
                : $"{skill.Id}  ({name})");

            if (skill.Id == wantSkillId) selectIdx = i;
            i++;
        }

        if (skillIds.Count > 0)
            skillOption.Selected = selectIdx >= 0 ? selectIdx : 0;
    }

    private static string ReadSkillId(OptionButton option, List<string> ids)
    {
        if (option.Selected < 0 || option.Selected >= ids.Count) return null;
        return ids[option.Selected];
    }

    private void OnPrimaryStatChanged()
    {
        string statId = ReadStatId(_primaryStatOption);
        PopulateSkillOptions(_primarySkillOption, _primarySkillIds, statId, null);
    }

    private void OnSecondaryStatChanged()
    {
        string statId = ReadStatId(_secondaryStatOption);
        PopulateSkillOptions(_secondarySkillOption, _secondarySkillIds, statId, null);
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
        foreach (var c in _classes)
            _list.AddItem(c.Id);
    }

    private void SelectFirst()
    {
        if (_classes.Count == 0) return;
        _list.Select(0);
        OnClassSelected(0);
    }

    private void OnClassSelected(long index)
    {
        if (index < 0 || index >= _classes.Count) return;
        _current = _classes[(int)index];
        LoadCurrentIntoUI();
    }

    private void LoadCurrentIntoUI()
    {
        var c = _current;

        _idEdit.Text = c.Id ?? "";
        _nameRuEdit.Text = c.Name.Ru ?? "";
        _nameEnEdit.Text = c.Name.En ?? "";

        // Primary stat + skill
        int pStatIdx = FindStatIndex(_primaryStatOption, c.PrimaryStat);
        if (pStatIdx >= 0) _primaryStatOption.Selected = pStatIdx;
        PopulateSkillOptions(_primarySkillOption, _primarySkillIds,
                             c.PrimaryStat, c.PrimarySkill);

        // Secondary stat + skill
        int sStatIdx = FindStatIndex(_secondaryStatOption, c.SecondaryStat);
        if (sStatIdx >= 0) _secondaryStatOption.Selected = sStatIdx;
        PopulateSkillOptions(_secondarySkillOption, _secondarySkillIds,
                             c.SecondaryStat, c.SecondarySkill);

        _weightSpin.Value = c.Weight;

        foreach (var statId in StatIds.All)
        {
            double v = 0;
            if (c.GrowthWeights != null && c.GrowthWeights.TryGetValue(statId, out var w))
                v = w;
            _growthBoxes[statId].Value = v;
        }

        // Warn если что-то не сошлось с текущими данными.
        var warnings = new List<string>();
        if (!string.IsNullOrEmpty(c.PrimaryStat)
            && !AdventurerDatabase.IsValidSkill(c.PrimarySkill))
            warnings.Add($"primarySkill '{c.PrimarySkill}' не найден");
        if (!string.IsNullOrEmpty(c.SecondaryStat)
            && !AdventurerDatabase.IsValidSkill(c.SecondarySkill))
            warnings.Add($"secondarySkill '{c.SecondarySkill}' не найден");
        if (warnings.Count > 0)
            SetStatus(string.Join("; ", warnings), StatusKind.Warning);
        else
            SetStatus("", StatusKind.Ok);
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
                var dict = new Dictionary<string, AdventurerClassInfo>();
                foreach (var c in _classes) dict[c.Id] = c;
                AdventurerDatabase.Install(classes: dict);

                string suffix = warnings.Count > 0
                    ? $" Предупреждения: {string.Join("; ", warnings)}"
                    : "";
                SetStatus($"Preview: {_classes.Count} классов в памяти (диск не тронут).{suffix}",
                          StatusKind.Warning);
            }
            catch (Exception e)
            {
                SetStatus($"Preview FAILED: {e.Message}", StatusKind.Error);
                GD.PushError($"[ClassesTab] Preview: {e}");
            }
            return;
        }

        try
        {
            var db = new AdventurerClassDatabase { Classes = _classes };
            JsonWriter.Write(ClassesPath, db);
        }
        catch (Exception e)
        {
            SetStatus($"Apply FAILED: {e.Message}", StatusKind.Error);
            GD.PushError($"[ClassesTab] Apply: {e}");
            return;
        }

        try { AdventurerDatabase.Load(); QuestDatabase.Load(); }
        catch (Exception e)
        {
            SetStatus($"Записано, но Load упал: {e.Message}", StatusKind.Error);
            GD.PushError($"[ClassesTab] Load after apply: {e}");
            return;
        }

        if (warnings.Count > 0)
        {
            SetStatus($"Applied ({_classes.Count}). Предупреждения: {string.Join("; ", warnings)}",
                      StatusKind.Warning);
        }
        else
        {
            SetStatus($"Applied: {_classes.Count} классов записано", StatusKind.Ok);
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
            SetStatus($"Reverted: {_classes.Count} классов перезагружено", StatusKind.Ok);
        }
        catch (Exception e)
        {
            SetStatus($"Revert: Load упал: {e.Message}", StatusKind.Error);
        }
    }

    private List<string> SyncUIToCurrent()
    {
        var warnings = new List<string>();
        var c = _current;

        c.Name = new LocalizedString { Ru = _nameRuEdit.Text, En = _nameEnEdit.Text };

        string primaryStat = ReadStatId(_primaryStatOption);
        string primarySkill = ReadSkillId(_primarySkillOption, _primarySkillIds);
        string secondaryStat = ReadStatId(_secondaryStatOption);
        string secondarySkill = ReadSkillId(_secondarySkillOption, _secondarySkillIds);

        // Проверка «primarySkill принадлежит primaryStat».
        if (!string.IsNullOrEmpty(primarySkill))
        {
            string actualStat = AdventurerDatabase.StatForSkill(primarySkill);
            if (actualStat != null && actualStat != primaryStat)
                warnings.Add($"primarySkill '{primarySkill}' принадлежит '{actualStat}', " +
                             $"а primaryStat = '{primaryStat}'");
        }

        if (!string.IsNullOrEmpty(secondarySkill))
        {
            string actualStat = AdventurerDatabase.StatForSkill(secondarySkill);
            if (actualStat != null && actualStat != secondaryStat)
                warnings.Add($"secondarySkill '{secondarySkill}' принадлежит '{actualStat}', " +
                             $"а secondaryStat = '{secondaryStat}'");
        }

        c.PrimaryStat = primaryStat;
        c.PrimarySkill = primarySkill;
        c.SecondaryStat = secondaryStat;
        c.SecondarySkill = secondarySkill;
        c.Weight = (int)_weightSpin.Value;

        var weights = new Dictionary<string, double>();
        foreach (var statId in StatIds.All)
            weights[statId] = NumericHelpers.Round4(_growthBoxes[statId].Value);
        c.GrowthWeights = weights;

        return warnings;
    }

    private void ReloadFromDisk()
    {
        try
        {
            var db = JsonLoader.Load<AdventurerClassDatabase>(ClassesPath);
            _classes.Clear();
            if (db?.Classes != null) _classes.AddRange(db.Classes);
            GD.Print($"[ClassesTab] ReloadFromDisk: загружено {_classes.Count} классов");
        }
        catch (Exception e)
        {
            GD.PushError($"[ClassesTab] Не удалось загрузить {ClassesPath}: {e}");
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