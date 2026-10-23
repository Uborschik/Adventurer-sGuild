namespace AdventurersGuild.Editor.BalanceLab;

#if TOOLS
using Godot;
using AdventurersGuild.Domain.Units;
using System;
using AdventurersGuild.Domain.Combat;
using System.Collections.Generic;

[Tool]
public partial class CombatLabCard : Control
{
    [Export] private Label nameLabel, classLabel, levelLabel, hpLabel, acLabel;
    [Export] private Button editBtn, removeBtn;

    public event Action<CombatLabCard> EditRequested;
    public event Action<CombatLabCard> RemoveRequested;

    public UnitConfigData Data { get; set; } = new();

    public override void _Ready()
    {
        editBtn.Pressed += () => EditRequested?.Invoke(this);
        removeBtn.Pressed += () => RemoveRequested?.Invoke(this);
    }

    public void UpdateUI()
    {
        if (string.IsNullOrEmpty(Data.SpeciesId)) return;

        nameLabel.Text = UnitDatabase.SpeciesName(Data.SpeciesId);
        classLabel.Text = UnitDatabase.ClassName(Data.ClassId);
        levelLabel.Text = $"Lvl {Data.Level}";

        var cls = UnitDatabase.GetClass(Data.ClassId);
        double end = Data.CustomStats.GetValueOrDefault("end", 10);
        double dex = Data.CustomStats.GetValueOrDefault("dex", 10);

        hpLabel.Text = $"{(int)(end * LevelBalance.Active.HpPerEndurance * (cls?.HpMultiplier ?? 1.0))}";
        acLabel.Text = CombatMath.CalculateAc(dex, end).ToString();
    }
}
#endif