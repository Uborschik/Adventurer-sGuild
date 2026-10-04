using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class TextSubstitution
{
    private static readonly Regex Pattern =
        new(@"\{(\w+)(?::(\w+))?\}", RegexOptions.Compiled);

    public static string Resolve(string template, string lang, IReadOnlyDictionary<string, LocalizedNoun> nouns)
    {
        if (string.IsNullOrEmpty(template)) return "";

        return Pattern.Replace(template, match =>
        {
            string key = match.Groups[1].Value;
            string caseCode = match.Groups[2].Success
                ? match.Groups[2].Value
                : "nom";

            if (nouns == null || !nouns.TryGetValue(key, out var noun) || noun == null)
                return match.Value;

            return noun.Get(lang, caseCode);
        });
    }
}