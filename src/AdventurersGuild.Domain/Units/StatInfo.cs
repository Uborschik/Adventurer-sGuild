namespace AdventurersGuild.Domain.Adventurers;

using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

public class StatInfo
{
    public string Id { get; set; }
    public LocalizedString Name { get; set; }
    public LocalizedString Description { get; set; }
    public string Icon { get; set; }
}

public class AdventurerStatDatabase
{
    public List<StatInfo> Stats { get; set; } = [];
}