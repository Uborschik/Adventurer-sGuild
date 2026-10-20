using System;

namespace AdventurersGuild.Core;

public class GameClock(GameTime start)
{
    public GameTime Now { get; private set; } = start;
    public event Action<GameTime> TimeAdvanced;

    public void Advance(GameTime delta)
    {
        if (delta.TotalMinutes <= 0) return;

        Now += delta;
        TimeAdvanced?.Invoke(Now);
    }

    public void AdvanceDays(int days) => Advance(GameTime.FromDays(days));
    public void AdvanceHours(int hours) => Advance(GameTime.FromHours(hours));
    public void AdvanceMinutes(int minutes) => Advance(GameTime.FromMinutes(minutes));
}