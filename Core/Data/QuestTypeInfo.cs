using System.Collections.Generic;

public class QuestTypeInfo
{
    public string Id { get; set; }
    public LocalizedString Name { get; set; }
    public int Weight { get; set; }
}

public class QuestTypeDatabase
{
    public List<QuestTypeInfo> Types { get; set; } = new();
}
