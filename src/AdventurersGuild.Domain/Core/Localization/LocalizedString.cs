namespace AdventurersGuild.Core.Localization;

public struct LocalizedString
{
    public string Ru { get; set; }
    public string En { get; set; }

    public string Get(string lang) => lang switch
    {
        "ru" => Ru ?? En ?? "",
        "en" => En ?? Ru ?? "",
        _ => Ru ?? En ?? ""
    };
}