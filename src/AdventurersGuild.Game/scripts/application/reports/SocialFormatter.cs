namespace AdventurersGuild.Application.Reports;

public static class SocialFormatter
{
    public static string FormatEncounter(string roleId, bool passed)
    {
        return (roleId, passed) switch
        {
            ("pilgrim", true) => "  [color=#7ad67a]КОНТАКТ:[/color] Встречен паломник. Путем переговоров получены ценные сведения о маршруте.\n",
            ("pilgrim", false) => "  [color=#ff6b6b]КОНТАКТ:[/color] Встречен паломник. Лицо отказалось предоставлять информацию.\n",

            ("merchant", true) => "  [color=#7ad67a]СНАБЖЕНИЕ:[/color] Встречен торговец. Закуплены припасы по выгодной цене.\n",
            ("merchant", false) => "  [color=#ff6b6b]СНАБЖЕНИЕ:[/color] Встречен торговец. Сделка не состоялась.\n",

            ("guard", true) => "  [color=#7ad67a]КПП:[/color] Проверка документов пройдена. Страж пропустил отряд.\n",
            ("guard", false) => "  [color=#ff6b6b]КПП:[/color] Страж проявил враждебность. Завязался бой.\n",

            _ => $"  [color=#aaa]ЗАПИСЬ:[/color] Встреча с {roleId}. Исход: {(passed ? "Успех" : "Провал")}.\n"
        };
    }
}