namespace AdventurersGuild.Editor.BalanceLab;

#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Domain.Units;
using AdventurersGuild.Domain.Game;

[Tool]
public partial class CombatLabPopup : Window
{
    [Export] private OptionButton speciesSelect, classSelect;
    [Export] private SpinBox levelInput;
    [Export] private CheckBox overrideCheck;
    [Export] private Button saveBtn, cancelBtn;

    public event Action<UnitConfigData> Saved;

    private List<SpeciesInfo> availableSpecies = [];
    private readonly List<ClassInfo> availableClasses = [];

    private VBoxContainer statsContainer;
    private GridContainer statsGrid;
    private readonly Dictionary<string, SpinBox> statInputs = [];

    private bool isEnemy;
    private bool isUpdatingUI;

    public override void _Ready()
    {
        CloseRequested += Hide;

        saveBtn.Pressed += OnSavePressed;
        cancelBtn.Pressed += Hide;

        // Подписываемся на именованные методы
        speciesSelect.ItemSelected += OnSpeciesSelected;
        classSelect.ItemSelected += OnClassSelected;
        levelInput.ValueChanged += OnLevelChanged;
        overrideCheck.Toggled += OnOverrideToggled;

        BuildStatsUI();
    }

    public override void _ExitTree()
    {
        // Отписываемся от встроенных сигналов нод, чтобы не оставлять висячих ссылок
        CloseRequested -= Hide;

        saveBtn.Pressed -= OnSavePressed;
        cancelBtn.Pressed -= Hide;

        speciesSelect.ItemSelected -= OnSpeciesSelected;
        classSelect.ItemSelected -= OnClassSelected;
        levelInput.ValueChanged -= OnLevelChanged;
        overrideCheck.Toggled -= OnOverrideToggled;

        // Отписываемся от динамически созданных SpinBox'ов
        foreach (var spinBox in statInputs.Values)
        {
            if (IsInstanceValid(spinBox))
                spinBox.ValueChanged -= OnStatValueChanged;
        }

        statInputs.Clear();
    }

    // --- Обработчики событий (вынесены из лямбд) ---

    private void OnSpeciesSelected(long index) => UpdateClasses();
    private void OnClassSelected(long index) => UpdateLevelRange();
    private void OnLevelChanged(double value) => PreviewStats();
    private void OnStatValueChanged(double value) => UpdateDerivedStats();

    private void OnOverrideToggled(bool toggled)
    {
        statsContainer.Visible = toggled;
        foreach (var spinBox in statInputs.Values)
            spinBox.Editable = toggled;
        UpdateDerivedStats();
    }

    private void BuildStatsUI()
    {
        statsContainer = new VBoxContainer
        {
            Visible = overrideCheck.ButtonPressed,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        var parent = overrideCheck.GetParent();
        if (parent != null)
        {
            parent.AddChild(statsContainer);
            parent.MoveChild(statsContainer, overrideCheck.GetIndex() + 1);
        }

        statsGrid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        statsGrid.AddThemeConstantOverride("h_separation", 10);
        statsGrid.AddThemeConstantOverride("v_separation", 5);
        statsContainer.AddChild(statsGrid);

        foreach (var statId in StatIds.All)
        {
            var label = new Label { Text = $"{UnitDatabase.StatName(statId).ToUpper()}:" };
            statsGrid.AddChild(label);

            var spinBox = new SpinBox
            {
                MinValue = 1,
                MaxValue = 999,
                Step = 1,
                Value = 10,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                Editable = overrideCheck.ButtonPressed
            };

            // Используем именованный метод
            spinBox.ValueChanged += OnStatValueChanged;

            statInputs[statId] = spinBox;
            statsGrid.AddChild(spinBox);
        }
    }

    public void Open(UnitConfigData data, bool enemy)
    {
        isEnemy = enemy;
        Title = isEnemy ? "Enemy Configuration" : "Hero Configuration";

        var all = UnitDatabase.Species.Values.OrderBy(s => s.Id).ToList();
        if (isEnemy) availableSpecies = all;
        else
        {
            var allowed = GameRules.AdventurerSpecies;
            availableSpecies = [.. all.Where(s => allowed.Contains(s.Id))];
        }

        speciesSelect.Clear();
        foreach (var s in availableSpecies) speciesSelect.AddItem(UnitDatabase.SpeciesName(s.Id));

        int sIdx = availableSpecies.FindIndex(s => s.Id == data.SpeciesId);
        speciesSelect.Selected = sIdx >= 0 ? sIdx : 0;

        UpdateClasses(data.ClassId);
        levelInput.Value = data.Level;
        overrideCheck.ButtonPressed = data.OverrideStats;

        isUpdatingUI = true;
        if (data.CustomStats.Count > 0)
        {
            foreach (var kvp in statInputs)
            {
                kvp.Value.Value = data.CustomStats.GetValueOrDefault(kvp.Key, 10);
            }
        }
        isUpdatingUI = false;

        UpdateDerivedStats();
        PopupCentered(new Vector2I(400, 500));
    }

    private void UpdateClasses(string forceClass = null)
    {
        classSelect.Clear();
        availableClasses.Clear();
        if (speciesSelect.Selected < 0) return;

        var species = availableSpecies[speciesSelect.Selected];
        foreach (var sc in species.Classes)
        {
            var cls = UnitDatabase.GetClass(sc.ClassId);
            if (cls != null)
            {
                availableClasses.Add(cls);
                classSelect.AddItem(UnitDatabase.ClassName(cls.Id));
            }
        }

        if (forceClass != null)
        {
            int idx = availableClasses.FindIndex(c => c.Id == forceClass);
            if (idx >= 0) classSelect.Selected = idx;
        }
        UpdateLevelRange();
    }

    private void UpdateLevelRange()
    {
        if (speciesSelect.Selected < 0 || classSelect.Selected < 0) return;
        var species = availableSpecies[speciesSelect.Selected];
        var sc = species.Classes.FirstOrDefault(c => c.ClassId == availableClasses[classSelect.Selected].Id);

        if (sc != null)
        {
            levelInput.MinValue = sc.MinLvl > 0 ? sc.MinLvl : 1;
            levelInput.MaxValue = sc.MaxLvl > 0 ? sc.MaxLvl : 60;
            if (levelInput.Value < levelInput.MinValue) levelInput.Value = levelInput.MinValue;
            if (levelInput.Value > levelInput.MaxValue) levelInput.Value = levelInput.MaxValue;
        }
        PreviewStats();
    }

    private void PreviewStats()
    {
        if (isUpdatingUI || speciesSelect.Selected < 0 || classSelect.Selected < 0 || overrideCheck.ButtonPressed) return;

        var species = availableSpecies[speciesSelect.Selected];
        var cls = availableClasses[classSelect.Selected];
        int lvl = (int)levelInput.Value;

        isUpdatingUI = true;
        Dictionary<string, double> stats;

        if (!isEnemy) stats = StatGrowth.AtLevel(cls.Id, species.Id, lvl);
        else
        {
            stats = new Dictionary<string, double>();
            foreach (var s in StatIds.All)
                stats[s] = GetNpcStat(species, lvl, s);
        }

        foreach (var kvp in statInputs)
        {
            kvp.Value.Value = (int)Math.Round(stats.GetValueOrDefault(kvp.Key, 10));
        }

        isUpdatingUI = false;
        UpdateDerivedStats();
    }

    private void UpdateDerivedStats()
    {
        // Здесь можно добавить логику обновления лейблов, если добавишь их в сцену
    }

    private void OnSavePressed()
    {
        var stats = new Dictionary<string, double>();
        foreach (var kvp in statInputs)
        {
            stats[kvp.Key] = kvp.Value.Value;
        }

        var data = new UnitConfigData
        {
            SpeciesId = availableSpecies[speciesSelect.Selected].Id,
            ClassId = availableClasses[classSelect.Selected].Id,
            Level = (int)levelInput.Value,
            OverrideStats = overrideCheck.ButtonPressed,
            CustomStats = stats
        };

        Saved?.Invoke(data);
        Hide();
    }

    private static double GetNpcStat(SpeciesInfo species, int level, string statId)
    {
        if (species?.GrowthWeights == null) return 10;
        var p = LevelBalance.Active;
        int lvls = Math.Max(0, level - 1);
        double weight = species.GrowthWeights.GetValueOrDefault(statId, p.Combat.DefaultGrowthWeight);
        return p.StatBaseValue + (p.StatStartPool + lvls * p.StatBaseSlope) * weight + species.StatBonuses.GetValueOrDefault(statId, 0);
    }
}
#endif