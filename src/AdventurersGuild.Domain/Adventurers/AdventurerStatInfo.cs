using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

namespace AdventurersGuild.Domain.Adventurers;

public class AdventurerStatInfo
{
    public string Id { get; set; }
    public LocalizedString Name { get; set; }
    public LocalizedString Description { get; set; }
    public string Icon { get; set; }
}

public class AdventurerStatDatabase
{
    public List<AdventurerStatInfo> Stats { get; set; } = new();
}