namespace AdventurersGuild.Domain.Quests;

using System.Collections.Generic;

public class CreatureContribution
{
    public List<string> Tags { get; set; } = new();
    public string PhaseId { get; set; }
    public int Duration { get; set; }
    public int Exp { get; set; }
    public int Weight { get; set; } = 10;
}