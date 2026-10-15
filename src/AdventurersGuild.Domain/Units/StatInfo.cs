using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

namespace AdventurersGuild.Domain.Adventurers;

public class StatInfo
{
    public string Id { get; set; }
    public LocalizedString Name { get; set; }
    public LocalizedString Description { get; set; }
    public string Icon { get; set; }
}

public class AdventurerStatDatabase
{
    public List<StatInfo> Stats { get; set; } = new();
}