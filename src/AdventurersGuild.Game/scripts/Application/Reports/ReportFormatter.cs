namespace AdventurersGuild.Application.Reports;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdventurersGuild.Application.Quests;
using AdventurersGuild.Core;
using AdventurersGuild.Data.Adventurer;
using AdventurersGuild.Data.Quest;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Quests;

public static class ReportFormatter
{
    // === Заголовок ===

    public static string QuestHeader(QuestModel quest)
    {
        string lang = Loc.Language;
        string target = Capitalize(QuestDatabase.GetCreature(quest.TargetId)?.Name?.Get(lang)
                                   ?? quest.TargetId ?? "?");
        string location = Capitalize(QuestDatabase.GetLocation(quest.LocationId)?.Name?.Get(lang)
                                     ?? quest.LocationId ?? "?");
        string party = string.Join(", ", quest.Party?.Select(a => a.FirstName) ?? Enumerable.Empty<string>());

        var sb = new StringBuilder();
        sb.Append($"[b]{quest.Name}[/b]\n");
        sb.Append($"[color=#aaa]Цель: {target}  ·  Место: {location}[/color]\n");
        sb.Append($"[color=#aaa]Отряд: {party}[/color]\n");
        return sb.ToString();
    }

    // === Взятие / отправление ===

    public static string QuestAccepted(QuestModel quest)
        => $"  [color=#7aa7ff]→[/color] Отряд готовится к походу. Выход: {quest.StartTime}\n";

    public static string QuestDeparted(QuestModel quest)
        => $"  [color=#7aa7ff]→[/color] Отряд отправился в путь.\n";

    // === Фазы ===

    public static string Phase(QuestPhaseInstance phase, PhaseOutcome outcome)
    {
        string role = phase.RoleId ?? phase.PhaseId;
        string mark = outcome.Passed
            ? "[color=#7ad67a]✓[/color]"
            : "[color=#ff6b6b]✗[/color]";

        var sb = new StringBuilder();
        sb.Append($"  {mark} [b]{role}[/b] ({phase.DurationMinutes}м)");

        if (phase.Critical && !outcome.Passed)
            sb.Append(" [color=#ff6b6b]— критично![/color]");
        else if (outcome.Passed)
            sb.Append($"  [color=#7ad67a]+{phase.ExpReward}xp[/color]");

        sb.Append('\n');
        return sb.ToString();
    }

    public static string AmbientPhase(QuestPhaseInstance phase, PhaseOutcome outcome)
    {
        string mark = outcome.Passed
            ? "[color=#7ad67a]✓[/color]"
            : "[color=#ff6b6b]✗[/color]";
        return $"  {mark} [i]по пути[/i] {phase.PhaseId} ({phase.DurationMinutes}м)\n";
    }

    public static IEnumerable<string> Rolls(PhaseOutcome outcome, int max = 8)
    {
        if (outcome.Rolls == null) yield break;

        int shown = 0;
        foreach (var r in outcome.Rolls)
        {
            if (shown++ >= max) yield break;

            string mark = r.Passed
                ? "[color=#7ad67a]✓[/color]"
                : "[color=#ff6b6b]✗[/color]";

            string skillName = UnitDatabase.GetSkill(r.SkillId)?.Name.Get(Loc.Language)
                               ?? r.SkillId;
            string adv = ExtractFirstName(r.BestAdvName);

            yield return
                $"      {mark} [b]{skillName}[/b] — {adv}: " +
                $"бросок {r.Roll} против {r.Threshold}  " +
                $"[color=#aaa](шанс {r.Chance:F0}%)[/color]\n";
        }
    }

    // === Травмы / смерть / level up ===

    public static string Wounded(UnitModel a, int days)
        => $"  [color=#ffb84d]⚠[/color] {a.FirstName} ранен на {days} д.\n";

    public static string Died(UnitModel a)
        => $"  [color=#ff6b6b]☠[/color] {a.FirstName} погиб.\n";

    public static string LevelUp(UnitModel a, int oldLevel, int newLevel)
        => $"  [color=#ffe066]★[/color] {a.FirstName} достиг L{newLevel}\n";

    // === Ночь ===

    public static string NightPeaceful()
        => "  [color=#7aa7ff]🌙[/color] Ночь прошла спокойно\n";

    public static string NightAttacked(bool defended)
        => defended
            ? "  [color=#7aa7ff]🌙[/color] Ночное нападение отбито\n"
            : "  [color=#ff6b6b]🌙[/color] Ночная атака! Отряд понёс потери\n";

    // === Финал ===

    public static string Finale(QuestModel quest)
    {
        var r = quest.Result;
        string role = r.Role switch
        {
            QuestGradeRole.Triumph => "[color=#7ad67a]Триумф[/color]",
            QuestGradeRole.Success => "[color=#7ad67a]Успех[/color]",
            QuestGradeRole.Failure => "[color=#ffb84d]Провал[/color]",
            QuestGradeRole.Disaster => "[color=#ff6b6b]Катастрофа[/color]",
            _ => "?"
        };

        int passed = quest.PhaseResults?.Count(p => p.Passed) ?? 0;
        int total = quest.PhaseResults?.Count ?? 0;
        int exp = quest.PhaseResults?.Sum(p => p.ExpEarned) ?? 0;

        return $"  [b]Итог: {role}[/b]  ·  фаз {passed}/{total}  ·  " +
               $"+{exp}xp  ·  +{r.GoldEarned}g  ·  +{r.GloryEarned}славы\n";
    }

    // === Утилиты ===

    private static string Capitalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpper(s[0]) + s.Substring(1);
    }

    private static string ExtractFirstName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return "-";
        int idx = fullName.IndexOf(' ');
        return idx > 0 ? fullName.Substring(0, idx) : fullName;
    }
}