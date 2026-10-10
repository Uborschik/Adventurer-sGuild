using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

namespace AdventurersGuild.Domain.Quests;

public class LocationInfo
{
    public string Id { get; set; }
    public LocalizedNoun Name { get; set; }
    public double DifficultyModifier { get; set; }
    public List<string> Standalone { get; set; } = new();
    public List<string> Creatures { get; set; } = new();
}

public class LocationDatabase
{
    public List<LocationInfo> Locations { get; set; } = new();
}