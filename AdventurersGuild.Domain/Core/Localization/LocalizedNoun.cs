namespace AdventurersGuild.Core.Localization;

public class LocalizedNoun
{
    public NounForms Ru { get; set; }
    public string En { get; set; }

    public string Get(string lang, string caseCode = "nom")
    {
        if (lang == "ru")
        {
            var form = Ru?.Get(caseCode);
            if (!string.IsNullOrEmpty(form)) return form;
            return Ru?.Nom ?? "";
        }
        return En ?? "";
    }
}

public class NounForms
{
    public string Nom { get; set; }
    public string Gen { get; set; }
    public string Dat { get; set; }
    public string Acc { get; set; }
    public string Ins { get; set; }
    public string Pre { get; set; }

    public string Get(string caseCode) => caseCode switch
    {
        "nom" => Nom,
        "gen" => Gen,
        "dat" => Dat,
        "acc" => Acc,
        "ins" => Ins,
        "pre" => Pre,
        _ => Nom,
    };
}