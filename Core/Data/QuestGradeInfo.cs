using System.Collections.Generic;

public class QuestGradeInfo
{
    public string Role { get; set; }
    public LocalizedString Name { get; set; }
    public float GoldMultiplier { get; set; }
    public float GloryMultiplier { get; set; }
    public bool CountsAsSuccess { get; set; }

    public string Id => Role?.ToLowerInvariant();
}

public class QuestGradeDatabase
{
    public List<QuestGradeInfo> Grades { get; set; } = new();
}
