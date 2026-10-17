namespace AdventurersGuild.Domain.Quests;

using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

public record PhaseInfo
{
    public string Id { get; init; }
    public LocalizedString Name { get; init; }
    public LocalizedString Description { get; init; }
}

public record InstructionInfo
{
    public string Id { get; init; }
}

public record PhaseDatabase
{
    public List<PhaseInfo> Phases { get; init; } = [];
}

public record InstructionDatabase
{
    public List<InstructionInfo> Instructions { get; init; } = [];
}