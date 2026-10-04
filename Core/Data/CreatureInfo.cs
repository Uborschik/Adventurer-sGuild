using System.Collections.Generic;

public class CreatureInfo
{
    public string Id { get; set; }
    public LocalizedNoun Name { get; set; }
    public List<string> Tags { get; set; }
    public string Tier { get; set; }
    public Dictionary<string, double> Weights { get; set; }
}

public class CreatureDatabase
{
    public List<CreatureInfo> Creatures { get; set; } = new();
}