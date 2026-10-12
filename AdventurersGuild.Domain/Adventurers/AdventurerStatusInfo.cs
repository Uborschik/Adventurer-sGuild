using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

namespace AdventurersGuild.Domain.Adventurers;

public class AdventurerStatusInfo
{
    public string Id { get; set; }
    public LocalizedString Name { get; set; }
    public bool BlocksAssignment { get; set; }
    public int? AutoRemoveAfterDays { get; set; }
}

public class AdventurerStatusDatabase
{
    public List<AdventurerStatusInfo> Statuses { get; set; } = new();
}