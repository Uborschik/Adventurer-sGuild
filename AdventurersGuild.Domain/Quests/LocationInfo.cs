namespace AdventurersGuild.Domain.Quests;

using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

public class LocationInfo
{
    public string Id { get; set; }
    public LocalizedNoun Name { get; set; }
    public double DifficultyModifier { get; set; }
    public int TravelDaysModifier { get; set; }
}

public class LocationDatabase
{
    public List<LocationInfo> Locations { get; set; } = new();
}