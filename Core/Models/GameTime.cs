using System;

public readonly struct GameTime : IEquatable<GameTime>, IComparable<GameTime>
{
    public const int MinutesPerDay = 1440;
    public const int MinutesPerDaylight = 960;
    public const int MinutesPerNight = 480;
    public const int MinutesPerHour = 60;
    public const int HoursPerDay = 24;

    public const int DayStartHour = 6;
    public const int DayEndHour = 22;

    public bool IsDaytime
    {
        get
        {
            int t = TotalMinutes % MinutesPerDay;
            int start = DayStartHour * MinutesPerHour;
            int end = start + MinutesPerDaylight;
            return t >= start && t < end;
        }
    }

    public readonly int TotalMinutes;

    public int Day => TotalMinutes / MinutesPerDay;
    public int Hour => (TotalMinutes % MinutesPerDay) / MinutesPerHour;
    public int Minute => TotalMinutes % MinutesPerHour;

    public GameTime(int totalMinutes)
    {
        TotalMinutes = totalMinutes;
    }

    public GameTime(int days, int hours, int minutes)
    {
        TotalMinutes = days * MinutesPerDay + hours * MinutesPerHour + minutes;
    }

    public static int MinutesToDays(int minutes) => minutes / MinutesPerDay;
    public static int DaysToMinutes(int days) => days * MinutesPerDay;

    public static int MinutesToHours(int minutes) => minutes / MinutesPerHour;
    public static int HoursToMinutes(int hours) => hours * MinutesPerHour;

    public static GameTime NextDayStart(GameTime now)
    {
        int todayStart = now.Day * MinutesPerDay + DayStartHour * MinutesPerHour;
        if (now.TotalMinutes < todayStart) return new GameTime(todayStart);
        return new GameTime(todayStart + MinutesPerDay);
    }

    public static GameTime FromDays(int days) => new(days * MinutesPerDay);
    public static GameTime FromHours(int hours) => new(hours * MinutesPerHour);
    public static GameTime FromMinutes(int minutes) => new(minutes);
    public static GameTime Zero { get; } = new(0);

    public static GameTime operator +(GameTime a, GameTime b) => new(a.TotalMinutes + b.TotalMinutes);
    public static GameTime operator -(GameTime a, GameTime b) => new(a.TotalMinutes - b.TotalMinutes);

    public static bool operator ==(GameTime a, GameTime b) => a.TotalMinutes == b.TotalMinutes;
    public static bool operator !=(GameTime a, GameTime b) => a.TotalMinutes != b.TotalMinutes;
    public static bool operator <(GameTime a, GameTime b) => a.TotalMinutes < b.TotalMinutes;
    public static bool operator >(GameTime a, GameTime b) => a.TotalMinutes > b.TotalMinutes;
    public static bool operator <=(GameTime a, GameTime b) => a.TotalMinutes <= b.TotalMinutes;
    public static bool operator >=(GameTime a, GameTime b) => a.TotalMinutes >= b.TotalMinutes;

    public bool Equals(GameTime other) => TotalMinutes == other.TotalMinutes;
    public override bool Equals(object obj) => obj is GameTime other && Equals(other);
    public override int GetHashCode() => TotalMinutes;
    public int CompareTo(GameTime other) => TotalMinutes.CompareTo(other.TotalMinutes);

    public override string ToString() => $"{Day:D3} {Hour:D2}:{Minute:D2}";
}