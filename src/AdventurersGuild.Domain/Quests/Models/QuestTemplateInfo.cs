namespace AdventurersGuild.Domain.Quests;

using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

public record QuestTemplateInfo
{
    public string Id { get; init; }
    public LocalizedString Name { get; init; }
    public LocalizedString Description { get; init; }
    public string LocationId { get; init; }
    public double ComplexityMultiplier { get; init; } = 1.0;

    public MainEncounterInfo MainEncounter { get; init; }
    public AmbientEncountersInfo AmbientEncounters { get; init; } = new();
}

public record MainEncounterInfo
{
    public string SpeciesId { get; init; }
    public string RoleId { get; init; }
    public PreCheckInfo? PreCheck { get; init; }
}

public readonly record struct PreCheckInfo(
    string SkillId,
    string TargetStat,
    string OnSuccess
);

public record AmbientEncountersInfo
{
    public MinMaxInfo Combat { get; init; } = new();
    public MinMaxInfo Social { get; init; } = new();
}

public readonly record struct MinMaxInfo(int Min, int Max);

public record QuestTemplateDatabase
{
    public List<QuestTemplateInfo> Templates { get; init; } = [];
}