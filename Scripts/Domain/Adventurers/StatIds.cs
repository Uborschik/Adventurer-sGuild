using System.Collections.Generic;

namespace AdventurersGuild.Domain.Adventurers;

public static class StatIds
{
    public const string Strength = "str";
    public const string Dexterity = "dex";
    public const string Endurance = "end";
    public const string Intelligence = "int";
    public const string Wisdom = "wis";
    public const string Charisma = "cha";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Strength, Dexterity, Endurance,
        Intelligence, Wisdom, Charisma
    };

    public static readonly IReadOnlyList<string> Active = new[]
    {
        Strength, Dexterity, Intelligence, Wisdom, Charisma
    };
}