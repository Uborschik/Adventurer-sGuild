namespace AdventurersGuild.Domain.Quests;

using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

public class QuestGradeInfo
{
    public string Role { get; set; } = string.Empty;
    public LocalizedString Name { get; set; }
    public float GoldMultiplier { get; set; }
    public float GloryMultiplier { get; set; }
    public bool CountsAsSuccess { get; set; }

    public string Id => Role?.ToLowerInvariant() ?? string.Empty;
}

public class QuestGradeDatabase
{
    public List<QuestGradeInfo> Grades { get; set; } = [];
}
