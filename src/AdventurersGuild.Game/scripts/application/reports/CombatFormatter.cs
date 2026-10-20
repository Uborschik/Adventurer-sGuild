namespace AdventurersGuild.Application.Reports;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdventurersGuild.Domain.Combat;

public static class CombatFormatter
{
    public static string FormatBattle(List<CombatEvent> events, string enemyName, int enemyLevel)
    {
        if (events == null || events.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.Append($"\n[color=#aaa]--- Запись о боевом столкновении ---[/color]\n");
        sb.Append($"[b]Противник:[/b] {enemyName} (Уровень угрозы: {enemyLevel})\n\n");

        int totalDamageToEnemy = 0;
        int totalDamageToParty = 0;

        foreach (var ev in events)
        {
            switch (ev.Type)
            {
                case CombatEventType.Start:
                    sb.Append($"    [color=#888]Противник вступает в бой. Состояние: {ev.TargetRemainingHp} ед. здоровья.[/color]\n\n");
                    break;

                case CombatEventType.RoundStart:
                    sb.Append($"  [b]Ход {ev.Round}:[/b]\n");
                    break;

                case CombatEventType.Attack:
                    if (ev.IsHit)
                    {
                        if (ev.ActorName == enemyName)
                        {
                            totalDamageToParty += ev.Damage;
                            sb.Append($"    [color=#ff6b6b]Удар[/color] Противник атаковал {ev.TargetName} и нанес {ev.Damage} ед. урона.\n");
                            sb.Append($"         [color=#888](Текущее состояние {ev.TargetName}: {ev.TargetRemainingHp}/{ev.TargetMaxHp} ед.)[/color]\n");
                        }
                        else
                        {
                            totalDamageToEnemy += ev.Damage;
                            sb.Append($"    [color=#7ad67a]Атака[/color] {ev.ActorName} нанес удар по противнику на {ev.Damage} ед. урона.\n");
                            sb.Append($"         [color=#888](Остаток здоровья врага: {ev.TargetRemainingHp}/{ev.TargetMaxHp} ед.)[/color]\n");
                        }
                    }
                    else
                    {
                        if (ev.ActorName != enemyName)
                            sb.Append($"    [color=#888]Промах[/color] {ev.ActorName} не смог пробить защиту противника.\n");
                        else
                            sb.Append($"    [color=#888]Уклонение[/color] {ev.TargetName} увернулся от атаки противника.\n");
                    }
                    break;

                case CombatEventType.Death:
                    sb.Append($"    [color=#ff6b6b][b]Потери![/b] {ev.ActorName} выведен из строя.[/color]\n");
                    break;

                case CombatEventType.Victory:
                    sb.Append($"\n  [color=#7ad67a][b]Итог боя:[/b] Противник уничтожен.[/color]\n");
                    break;

                case CombatEventType.Defeat:
                    sb.Append($"\n  [color=#ff6b6b][b]Итог боя:[/b] Отряд разбит и отступает.[/color]\n");
                    break;

                case CombatEventType.Timeout:
                    sb.Append($"\n  [color=#ffb84d][b]Итог боя:[/b] Противник скрылся, бой затянулся.[/color]\n");
                    break;
            }
        }

        sb.Append($"\n  [color=#aaa]Сводка: Нанесено урона врагу — {totalDamageToEnemy}. Получено урона отрядом — {totalDamageToParty}.[/color]\n");
        sb.Append($"[color=#aaa]-----------------------------------[/color]\n\n");

        return sb.ToString();
    }
}