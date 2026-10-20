namespace AdventurersGuild.Domain.Combat;

public enum CombatEventType
{
    Start,
    RoundStart,
    Attack,
    Death,
    Victory,
    Defeat,
    Timeout,
    NoParty
}

public readonly record struct CombatEvent(
    int Round,
    CombatEventType Type,
    string ActorName,
    string TargetName,
    int Roll,
    int TotalAttack,
    int TargetAc,
    int Damage,
    int TargetRemainingHp,
    int TargetMaxHp,
    bool IsHit
)
{
    // === Статические конструкторы (как Vector3.Zero) ===

    public static CombatEvent NoParty()
        => new(0, CombatEventType.NoParty, "", "", 0, 0, 0, 0, 0, 0, false);

    public static CombatEvent Start(string enemyName, int currentHp, int maxHp)
        => new(0, CombatEventType.Start, enemyName, "", 0, 0, 0, 0, currentHp, maxHp, false);

    public static CombatEvent RoundStart(int round)
        => new(round, CombatEventType.RoundStart, "", "", 0, 0, 0, 0, 0, 0, false);

    public static CombatEvent Attack(int round, string actor, string target, int roll, int total, int ac, int dmg, int remainHp, int maxHp, bool isHit)
        => new(round, CombatEventType.Attack, actor, target, roll, total, ac, dmg, remainHp, maxHp, isHit);

    public static CombatEvent Death(int round, string actorName)
        => new(round, CombatEventType.Death, actorName, "", 0, 0, 0, 0, 0, 0, false);

    public static CombatEvent Victory(int round, string enemyName)
        => new(round, CombatEventType.Victory, enemyName, "", 0, 0, 0, 0, 0, 0, false);

    public static CombatEvent Defeat(int round)
        => new(round, CombatEventType.Defeat, "", "", 0, 0, 0, 0, 0, 0, false);

    public static CombatEvent Timeout(int round)
        => new(round, CombatEventType.Timeout, "", "", 0, 0, 0, 0, 0, 0, false);
}