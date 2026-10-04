using System.Collections.Generic;

public static class StatusIds
{
    public const string Free = "free";
    public const string OnQuest = "on_quest";
    public const string InParty = "in_party";
    public const string Wounded = "wounded";
    public const string Dead = "dead";
}

public static class StatIds
{
    public const string Might = "might";
    public const string Finesse = "finesse";
    public const string Wits = "wits";
    public const string Presence = "presence";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Might, Finesse, Wits, Presence
    };
}