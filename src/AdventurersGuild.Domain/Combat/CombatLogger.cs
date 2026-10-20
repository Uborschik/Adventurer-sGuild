namespace AdventurersGuild.Domain.Combat;

public static class CombatLogger
{
    public static string Header(CombatActor enemy, int level)
        => $"--- Бой: {enemy.Name} (Ур. {level}) [HP: {enemy.MaxHp}, AC: {enemy.Ac}] ---";

    public static string Initiative(CombatActor actor, int initiative)
        => $"--- Инициатива: {actor.Name} ({initiative})";

    public static string RoundStart(int round)
        => $"[Раунд {round}]";

    public static string Attack(CombatActor attacker, CombatActor target, AttackResult res)
    {
        if (res.Hit)
            return $"  {attacker.Name} атакует {target.Name}: {attacker.Attack:F1}+d20({res.Roll})={res.Total} vs AC {target.Ac}. ПОПАДАНИЕ! Урон: {res.Damage}. (HP: {target.CurrentHp}/{target.MaxHp})";

        return $"  {attacker.Name} атакует {target.Name}: {attacker.Attack:F1}+d20({res.Roll})={res.Total} vs AC {target.Ac}. Промах.";
    }

    public static string Victory(CombatActor enemy)
        => $"Победа! {enemy.Name} повержен.";

    public static string Defeat()
        => "Поражение. Отряд пал в бою.";

    public static string Timeout(int maxRounds)
        => $"Бой затянулся ({maxRounds} раундов). Отряд отступает.";
}