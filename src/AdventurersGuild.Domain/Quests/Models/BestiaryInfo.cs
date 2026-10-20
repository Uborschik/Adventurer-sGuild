namespace AdventurersGuild.Domain.Quests;

using System.Collections.Generic;

public record BestiaryInfo
{
    public string LocationId { get; init; }

    public Dictionary<string, Dictionary<string, int>> Roles { get; init; } = [];
}

public record BestiaryDatabase
{
    public List<BestiaryInfo> Habitats { get; init; } = [];
}