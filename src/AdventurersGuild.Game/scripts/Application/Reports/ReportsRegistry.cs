namespace AdventurersGuild.Application.Reports;

using System;
using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Core;
using AdventurersGuild.Domain.Quests;
using AdventurersGuild.Domain.Reports;

public class ReportsRegistry
{
    private const int MaxDays = 90;

    private readonly Stack<DayReport> days = new();

    public event Action Changed;
    public IEnumerable<DayReport> Days => days;

    // === Поиск ===

    private DayReport EnsureDay(int globalDay)
    {
        foreach (var day in days)
            if (day.GlobalDay == globalDay) return day;

        var newDay = new DayReport { GlobalDay = globalDay };
        days.Push(newDay);
        return newDay;
    }

    private QuestDayReport GetOrCreate(int globalDay, QuestModel quest)
    {
        var day = EnsureDay(globalDay);

        var report = day.Quests.FirstOrDefault(q => q.QuestId == quest.Id);
        if (report != null) return report;

        // report = new QuestDayReport
        // {
        //     QuestId = quest.Id,
        //     QuestName = quest.Name,
        //     BlueprintId = quest.BlueprintId,
        //     TargetId = quest.TargetId,
        //     LocationId = quest.LocationId,
        //     DayNumber = quest.DaysSpent + 1,
        // };

        report.Lines.Add(ReportFormatter.QuestHeader(quest));
        day.Quests.Add(report);
        return report;
    }

    // === Запись ===

    public void Append(int globalDay, QuestModel quest, string line)
    {
        if (string.IsNullOrEmpty(line)) return;

        var report = GetOrCreate(globalDay, quest);
        report.Lines.Add(line);
        Changed?.Invoke();
    }

    public void AppendMany(int globalDay, QuestModel quest, IEnumerable<string> lines)
    {
        if (lines == null) return;

        var arr = lines.Where(l => !string.IsNullOrEmpty(l)).ToArray();
        if (arr.Length == 0) return;

        var report = GetOrCreate(globalDay, quest);
        report.Lines.AddRange(arr);
        Changed?.Invoke();
    }

    public void MarkFinal(int globalDay, QuestModel quest)
    {
        var report = GetOrCreate(globalDay, quest);
        if (report.IsFinal) return;

        report.IsFinal = true;
        Changed?.Invoke();
    }

    // === Обрезка истории ===

    public void Trim(GameTime now)
    {
        if (days.Count <= MaxDays) return;

        int cutoff = now.Day - MaxDays;

        var buffer = days.ToArray();
        days.Clear();

        for (int i = buffer.Length - 1; i >= 0; i--)
        {
            if (buffer[i].GlobalDay >= cutoff)
                days.Push(buffer[i]);
        }

        Changed?.Invoke();
    }
}