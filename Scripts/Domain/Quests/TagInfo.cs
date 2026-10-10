namespace AdventurersGuild.Domain.Quests;

using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

public class TagInfo
{
    public string Id { get; set; }
    public LocalizedString Name { get; set; }
}

public class TagDatabase
{
    public List<TagInfo> Tags { get; set; } = new();
}