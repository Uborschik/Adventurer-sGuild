namespace AdventurersGuild.Domain.Quests;

using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

public record LocationInfo
{
    public string Id { get; init; }
    public LocalizedNoun Name { get; init; }
    public double DifficultyModifier { get; init; }
    public int TravelDaysModifier { get; init; }
}

public record LocationDatabase
{
    public List<LocationInfo> Locations { get; init; } = [];
}