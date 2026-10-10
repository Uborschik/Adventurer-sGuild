namespace AdventurersGuild.Domain.Quests;

using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

public class Blueprint
{
    public string Id { get; set; }
    public LocalizedString Name { get; set; }
    public LocalizedString Description { get; set; }
    public RangeInt AmbientRange { get; set; } = new(0, 0);
    public int TravelDays { get; set; }
    public int BaseGold { get; set; }
    public int BaseGlory { get; set; }
    public double DifficultyModifier { get; set; }
    public List<BlueprintRole> Roles { get; set; } = new();
}

public class BlueprintRole
{
    public string Id { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool Required { get; set; }
    public List<BlueprintEdge> Incoming { get; set; } = new();
    public List<BlueprintOutgoing> Outgoing { get; set; } = new();
}

public readonly record struct BlueprintEdge(string From, EdgeCondition Condition, double DcDelta, int DurationDeltaMinutes);

public readonly record struct BlueprintOutgoing(string To, EdgeCondition Condition, double DcDelta, int DurationDeltaMinutes);

public enum EdgeCondition
{
    Any = 0,
    Pass = 1,
    Fail = 2
}

public readonly record struct RangeInt(int Min, int Max);

public class BlueprintDatabase
{
    public List<Blueprint> Blueprints { get; set; } = new();
}