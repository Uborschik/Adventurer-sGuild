using System.Collections.Generic;

public class AdventurerClassInfo
{
    public string Id { get; set; }
    public LocalizedString Name { get; set; }
    public string PrimaryStat { get; set; }
    public string PrimarySkill { get; set; }
    public string SecondaryStat { get; set; }
    public string SecondarySkill { get; set; }
    public Dictionary<string, double> GrowthWeights { get; set; }
    public int Weight { get; set; }
}

public class AdventurerClassDatabase
{
    public List<AdventurerClassInfo> Classes { get; set; } = new();
}