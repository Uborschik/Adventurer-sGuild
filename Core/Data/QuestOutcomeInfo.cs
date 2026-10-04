using System.Collections.Generic;

public class QuestOutcomeInfo
{
    public string GradeRole { get; set; }
    public int DeathChance { get; set; }
    public List<StatusChanceInfo> Statuses { get; set; }
}

public class StatusChanceInfo
{
    public string Id { get; set; }
    public int Chance { get; set; }
    public int DurationDaysMin { get; set; }
    public int DurationDaysMax { get; set; }
}

public class QuestOutcomeDatabase
{
    public List<QuestOutcomeInfo> Outcomes { get; set; } = new();
}