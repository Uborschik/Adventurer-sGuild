namespace AdventurersGuild.Domain.Quests;

using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

public class QuestPhaseTemplate
{
    public string Id { get; set; }
    public LocalizedString Name { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<PhasePath> Paths { get; set; } = new();
}

public class PhasePath
{
    public List<PhaseCheck> Checks { get; set; } = new();
}

public class PhaseCheck
{
    public string Skill { get; set; }
    public List<string> Resistance { get; set; } = new();
}

public class PhaseSolution
{
    public Dictionary<string, double> Skills { get; set; } = new();
}

public class QuestPhaseDatabase
{
    public List<QuestPhaseTemplate> Phases { get; set; } = new();
}