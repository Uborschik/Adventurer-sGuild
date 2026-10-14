
namespace AdventurersGuild.Application.Quests;

using System.Collections.Generic;
using AdventurersGuild.Domain.Quests;

public class QuestGenerationResult
{
    public string StartRoleId { get; set; }
    public List<QuestPhaseInstance> Phases { get; set; } = new();
    public List<QuestPhaseInstance> AmbientPhases { get; set; } = new();
}