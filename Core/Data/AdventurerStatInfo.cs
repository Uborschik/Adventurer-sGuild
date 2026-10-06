using System.Collections.Generic;

public class AdventurerStatInfo
{
    public string Id { get; set; }
    public LocalizedString Name { get; set; }
    public LocalizedString Description { get; set; }
    public List<string> Skills { get; set; } = new();
}

public class AdventurerStatDatabase
{
    public List<AdventurerStatInfo> Stats { get; set; } = new();
}