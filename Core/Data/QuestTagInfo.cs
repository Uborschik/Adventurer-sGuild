using System.Collections.Generic;

public class QuestTagInfo
{
    public string Id { get; set; }
    public Dictionary<string, double> Weights { get; set; }
    public string Tier { get; set; }
}

public class QuestTagDatabase
{
    public List<QuestTagInfo> Tags { get; set; } = new();
}