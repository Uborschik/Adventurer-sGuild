namespace AdventurersGuild.Application.Reports;

using System.Linq;
using System.Text;
using AdventurersGuild.Core;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Quests;

public static class ReportFormatter
{
    // === Заголовок ===

    public static string QuestHeader(QuestModel quest)
    {
        string lang = Loc.Language;

        // Ищем вид главного столкновения для заголовка
        var mainEnc = quest.Encounters?.FirstOrDefault(e => e.IsMainEncounter);
        var target = mainEnc != null
            ? Capitalize(UnitDatabase.GetSpecies(mainEnc.SpeciesId)?.Name?.Get(lang) ?? mainEnc.SpeciesId)
            : "?";

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

    // === Столкновения (пока простой текст) ===

    public static string Encounter(QuestEncounterInstance enc, bool passed)
    {
        string mark = passed ? "[color=#7ad67a]✓[/color]" : "[color=#ff6b6b]✗[/color]";
        string role = enc.RoleId ?? "Событие";
        return $"  {mark} [b]{role}[/b] (L{enc.Level})\n";
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
        var role = r.Role switch
        {
            QuestGradeRole.Triumph => "[color=#7ad67a]Триумф[/color]",
            QuestGradeRole.Success => "[color=#7ad67a]Успех[/color]",
            QuestGradeRole.Failure => "[color=#ffb84d]Провал[/color]",
            QuestGradeRole.Disaster => "[color=#ff6b6b]Катастрофа[/color]",
            _ => "?"
        };

        int passed = quest.Results?.Count(p => p.Passed) ?? 0;
        int total = quest.Encounters?.Count ?? 0;

        return $"  [b]Итог: {role}[/b]  ·  встреч {passed}/{total}  ·  " +
               $"+{r.GoldEarned}g  ·  +{r.GloryEarned}славы\n";
    }

    // === Утилиты ===

    private static string Capitalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpper(s[0]) + s.Substring(1);
    }
}