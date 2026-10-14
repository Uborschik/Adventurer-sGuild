namespace AdventurersGuild.Application.Quests;

using System.Collections.Generic;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Quests;

public class QuestContext
{
    public required Blueprint Blueprint { get; init; }
    public required CreatureInfo Target { get; init; }
    public required LocationInfo Location { get; init; }
    public required IReadOnlyList<AmbientCandidate> AmbientPool { get; init; }
    public required int CodeLevel { get; init; }
    public required AdventurerBalanceProfile AdventurerBalance { get; init; }

    public double ReferenceMaxStat =>
        AdventurerBalance.StatStartPool + (CodeLevel - 1) * AdventurerBalance.StatBaseSlope;
}