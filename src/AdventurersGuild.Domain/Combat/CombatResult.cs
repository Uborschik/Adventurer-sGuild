#nullable enable

using System.Collections.Generic;

namespace AdventurersGuild.Domain.Combat;

public class CombatResult
{
    public bool PartyWon { get; set; }
    public List<CombatEvent> Events { get; set; } = [];
    public Dictionary<string, WoundLevel> AdventurerWounds { get; set; } = [];
}