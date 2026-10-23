namespace AdventurersGuild.Application.Reports;

using System.Linq;
using System.Text;
using AdventurersGuild.Core;
using AdventurersGuild.Domain.Units;
using AdventurersGuild.Domain.Quests;

public static class ReportFormatter
{
    // === Заголовок ===

    public static string QuestHeader(QuestModel quest)
    {
        string lang = Loc.Language;

        var mainEnc = quest.Encounters?.FirstOrDefault(e => e.IsMainEncounter);
        var primaryUnit = mainEnc?.Units?.FirstOrDefault();

        var target = primaryUnit != null
            ? Capitalize(UnitDatabase.GetSpecies(primaryUnit.SpeciesId)?.Name?.Get(lang) ?? primaryUnit.SpeciesId)
            : "Не определено";

        string location = Capitalize(QuestDatabase.GetLocation(quest.LocationId)?.Name?.Get(lang)
                                     ?? quest.LocationId ?? "Неизвестно");
        string party = string.Join(", ", quest.Party?.Select(a => a.FirstName) ?? Enumerable.Empty<string>());

        var sb = new StringBuilder();
        sb.Append($"[b]ДОСЬЕ КОНТРАКТА: {quest.Name}[/b]\n");
        sb.Append($"[color=#aaa]Объект: {target} | Локация: {location}[/color]\n");
        sb.Append($"[color=#aaa]Личный состав отряда: {party}[/color]\n");
        sb.Append("[color=#555]--------------------------------------------------[/color]\n");
        return sb.ToString();
    }

    // === Взятие / отправление ===

    public static string QuestAccepted(QuestModel quest)
        => $"  [color=#7aa7ff]ЗАПИСЬ:[/color] Контракт принят в работу. Время выдвижения: {quest.StartTime}.\n";

    public static string QuestDeparted(QuestModel quest)
        => $"  [color=#7aa7ff]ЗАПИСЬ:[/color] Отряд покинул расположение гильдии и выдвинулся к месту выполнения.\n";

    // === Столкновения ===

    public static string Encounter(QuestEncounterInstance enc, bool passed)
    {
        string result = passed ? "[color=#7ad67a]УСПЕШНО[/color]" : "[color=#ff6b6b]ПРОВАЛ[/color]";

        var primaryUnit = enc.Units?.FirstOrDefault();
        string role = primaryUnit?.RoleId ?? "Непредвиденное событие";
        int level = primaryUnit?.Level ?? 0;

        return $"  [color=#aaa]Инцидент:[/color] [b]{role}[/b] (Уровень угрозы: {level}). Исход: {result}.\n";
    }

    // === Травмы / смерть / level up ===

    public static string Wounded(UnitModel a, int days)
        => $"  [color=#ffb84d]УБЫЛЬ:[/color] {a.FirstName} получил ранение. Срок потери боеспособности: {days} дн.\n";

    public static string Died(UnitModel a)
        => $"  [color=#ff6b6b]БЕЗВОЗВРАТНЫЕ ПОТЕРИ:[/color] {a.FirstName} погиб при исполнении контракта.\n";

    public static string LevelUp(UnitModel a, int oldLevel, int newLevel)
        => $"  [color=#ffe066]ОТМЕЧЕНО:[/color] {a.FirstName} повысил квалификацию до ранга L{newLevel}.\n";

    // === Ночь ===

    public static string NightPeaceful()
        => "  [color=#7aa7ff]ЛАГЕРЬ:[/color] Ночь прошла без происшествий. Противник не обнаружен.\n";

    public static string NightAttacked(bool defended)
        => defended
            ? "  [color=#7aa7ff]ЛАГЕРЬ:[/color] Ночное нападение отражено. Потерь нет.\n"
            : "  [color=#ff6b6b]ЛАГЕРЬ:[/color] Внезапная ночная атака! Отряд понес потери.\n";

    // === Финал ===

    public static string Finale(QuestModel quest)
    {
        var r = quest.Result;
        var status = r.Role switch
        {
            QuestGradeRole.Triumph => "[color=#7ad67a]ТРИУМФ (Превосходно)[/color]",
            QuestGradeRole.Success => "[color=#7ad67a]ВЫПОЛНЕНО[/color]",
            QuestGradeRole.Failure => "[color=#ffb84d]ПРОВАЛЕНО[/color]",
            QuestGradeRole.Disaster => "[color=#ff6b6b]КАТАСТРОФА[/color]",
            _ => "Неизвестно"
        };

        int passed = quest.Results?.Count(p => p.Passed) ?? 0;
        int total = quest.Encounters?.Count ?? 0;

        var sb = new StringBuilder();
        sb.Append("[color=#555]--------------------------------------------------[/color]\n");
        sb.Append($"[b]ИТОГОВЫЙ РАПОРТ:[/b] Статус контракта — {status}.\n");
        sb.Append($"[color=#aaa]Решено инцидентов: {passed} из {total}.[/color]\n");
        sb.Append($"[color=#aaa]Начислено вознаграждение: {r.GoldEarned} золота, {r.GloryEarned} славы.[/color]\n\n");

        return sb.ToString();
    }

    // === Утилиты ===

    private static string Capitalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpper(s[0]) + s[1..];
    }
}