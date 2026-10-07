using System;
using System.Collections.Generic;

public class AdventurerModel
{
    public event Action StatusChanged;
    public event Action ProgressChanged;

    public string Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string ClassId { get; }
    public string RaceId { get; }
    public Dictionary<string, double> Stats { get; private set; }
    public Level Level { get; private set; }
    public string StatusId { get; private set; } = StatusIds.Free;
    public GameTime? StatusExpiresAt { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    public AdventurerModel(string id, string firstName, string lastName, string classId, string raceId, Dictionary<string, double> stats, int level)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        ClassId = classId;
        RaceId = raceId;
        Stats = stats;
        Level = Level.New(level);
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