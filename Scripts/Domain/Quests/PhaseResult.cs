namespace AdventurersGuild.Domain.Quests;

using System.Collections.Generic;
using AdventurersGuild.Application.Quests;

public struct PhaseResult
{
    public QuestPhaseInstance Phase;
    public bool Passed;
    public bool EffectiveCritical;
    public int ExpEarned;
    public int DayCompleted;
    public int Attempts;
    public List<SkillRollResult> Rolls;
}