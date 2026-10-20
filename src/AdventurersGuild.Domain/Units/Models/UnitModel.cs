namespace AdventurersGuild.Domain.Units;

using System;
using System.Collections.Generic;
using AdventurersGuild.Core;

public class UnitModel
{
    public event Action StatusChanged;
    public event Action ProgressChanged;

    public string Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string ClassId { get; }
    public string RaceId { get; }
    public Dictionary<string, double> Stats { get; private set; }
    public int MaxHp { get; private set; }
    public int CurrentHp { get; private set; }
    public Level Level { get; private set; }
    public string StatusId { get; private set; } = StatusIds.Free;
    public GameTime? StatusExpiresAt { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    public UnitModel(string id, string firstName, string lastName, string classId, string raceId, Dictionary<string, double> stats, int level)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        ClassId = classId;
        RaceId = raceId;
        Stats = stats;
        Level = Level.New(level);

        var cls = UnitDatabase.GetClass(classId);
        var p = LevelBalance.Active;

        double end = Stats.GetValueOrDefault("end", 10);
        double baseHp = end * p.HpPerEndurance;

        double multiplier = cls?.HpMultiplier ?? 1.0;

        MaxHp = (int)(baseHp * multiplier);
        CurrentHp = MaxHp;
    }

    // === Статусы (без изменений) ===

    public bool HasStatus(string statusId) => StatusId == statusId;

    public void SetStatus(string statusId, GameTime? expiresAt = null)
    {
        if (string.IsNullOrEmpty(statusId)) return;
        if (StatusId == statusId && StatusExpiresAt == expiresAt) return;

        StatusId = statusId;
        StatusExpiresAt = expiresAt;
        StatusChanged?.Invoke();
    }

    public void SetCurrentHp(int hp) => CurrentHp = Math.Clamp(hp, 0, MaxHp);

    public void TakeDamage(int damage) => CurrentHp = Math.Max(0, CurrentHp - damage);

    public void Heal(int amount) => CurrentHp = Math.Min(MaxHp, CurrentHp + amount);

    public void ResetHp() => CurrentHp = MaxHp;

    private bool HasStatusExpired(GameTime now)
        => StatusExpiresAt.HasValue && now >= StatusExpiresAt.Value;

    public bool ShouldBeRemoved(GameTime now)
        => StatusId == StatusIds.Dead && HasStatusExpired(now);

    public void Tick(GameTime now)
    {
        if (!HasStatusExpired(now)) return;
        if (StatusId == StatusIds.Dead) return;

        SetStatus(StatusIds.Free);
    }

    // === Прокачка ===

    public void SetLevel(Level newLevel)
    {
        Level = newLevel;
        ProgressChanged?.Invoke();
    }

    public void SetStats(Dictionary<string, double> newStats) => Stats = newStats;
}