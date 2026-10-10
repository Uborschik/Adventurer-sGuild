using System.Collections.Generic;

namespace AdventurersGuild.Domain.Adventurers;

public static class SkillIds
{
    public const string Athletics = "athletics";
    public const string Intimidation = "intimidation";
    public const string Stealth = "stealth";
    public const string Acrobatics = "acrobatics";
    public const string Grit = "grit";
    public const string Fortitude = "fortitude";
    public const string Arcana = "arcana";
    public const string Investigation = "investigation";
    public const string Perception = "perception";
    public const string Survival = "survival";
    public const string Persuasion = "persuasion";
    public const string Deception = "deception";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Athletics, Intimidation, Stealth, Acrobatics,
        Grit, Fortitude, Arcana, Investigation,
        Perception, Survival, Persuasion, Deception
    };
}