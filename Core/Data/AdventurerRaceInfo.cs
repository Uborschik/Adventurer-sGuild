using System.Collections.Generic;

public class AdventurerRaceInfo
{
    public string Id { get; set; }
    public LocalizedString Name { get; set; }
    public Dictionary<string, int> StatBonuses { get; set; }
    public RaceWeightInfo Weight { get; set; }
}

public class RaceWeightInfo
{
    public int Population { get; set; }
    public Dictionary<string, int> Classes { get; set; }
}

public class AdventurerRaceDatabase
{
    public List<AdventurerRaceInfo> Races { get; set; } = new();
}
