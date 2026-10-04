using System.Collections.Generic;

public class AdventurerClassInfo
{
    public string Id { get; set; }
    public LocalizedString Name { get; set; }
    public string PrimaryStat { get; set; }
    public int HitDie { get; set; }
    public int Weight { get; set; }
}

public class AdventurerClassDatabase
{
    public List<AdventurerClassInfo> Classes { get; set; } = new();
}