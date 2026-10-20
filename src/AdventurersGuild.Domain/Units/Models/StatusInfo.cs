namespace AdventurersGuild.Domain.Units;

using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

public class StatusInfo
{
    public string Id { get; set; } = string.Empty;
    public LocalizedString Name { get; set; }
    public bool BlocksAssignment { get; set; }
    public int? AutoRemoveAfterDays { get; set; }
}

public class AdventurerStatusDatabase
{
    public List<StatusInfo> Statuses { get; set; } = new();
}