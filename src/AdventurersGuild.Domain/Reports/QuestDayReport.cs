namespace AdventurersGuild.Domain.Reports;

using System.Collections.Generic;

public class QuestDayReport
{
    public string QuestId { get; init; }
    public string QuestName { get; init; }
    public string TemplateId { get; set; }
    public string TargetId { get; init; }
    public string LocationId { get; init; }
    public int DayNumber { get; init; }

    public List<string> Lines { get; } = [];
    public bool IsFinal { get; set; }
}