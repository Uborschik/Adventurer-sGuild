using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

namespace AdventurersGuild.Domain.Quests;

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
