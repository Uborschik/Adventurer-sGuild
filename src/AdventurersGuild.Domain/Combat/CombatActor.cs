namespace AdventurersGuild.Domain.Combat;

using AdventurersGuild.Domain.Units;

public sealed class CombatActor
{
    public string Name { get; init; } = string.Empty;
    public int MaxHp { get; init; }
    public int CurrentHp { get; set; }
    public int Ac { get; init; }
    public double Attack { get; init; }
    public double Initiative { get; init; }

    public bool IsEnemy { get; init; }
    public UnitModel OriginalAdventurer { get; init; }

    public bool IsAlive => CurrentHp > 0;

    public void TakeDamage(int damage)
    {
        CurrentHp = System.Math.Max(0, CurrentHp - damage);
    }
}

public readonly record struct AttackResult(int Roll, int Total, int Damage, bool Hit);