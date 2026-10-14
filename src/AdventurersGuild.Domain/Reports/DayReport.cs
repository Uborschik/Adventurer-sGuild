namespace AdventurersGuild.Domain.Reports;

using System.Collections.Generic;

public class DayReport
{
    public int GlobalDay { get; init; }
    public List<QuestDayReport> Quests { get; } = new();
}