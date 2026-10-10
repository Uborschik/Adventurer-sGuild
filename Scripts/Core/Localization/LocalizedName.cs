namespace AdventurersGuild.Core.Localization;

public readonly struct LocalizedName
{
    public string En { get; init; }
    public string Ru { get; init; }

    public string Get(string lang) => lang switch
    {
        "ru" => Ru,
        "en" => En,
        _ => En
    };
}
