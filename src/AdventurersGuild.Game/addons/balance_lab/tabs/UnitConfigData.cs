namespace AdventurersGuild.Editor.BalanceLab;

using System.Collections.Generic;

public class UnitConfigData
{
    public string SpeciesId { get; set; } = "";
    public string ClassId { get; set; } = "";
    public int Level { get; set; } = 1;
    public bool OverrideStats { get; set; } = false;
    public Dictionary<string, double> CustomStats { get; set; } = [];
}