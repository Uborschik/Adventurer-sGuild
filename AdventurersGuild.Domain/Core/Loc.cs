namespace AdventurersGuild.Core;

using System;
using System.Collections.Generic;

public static class Loc
{
    private const string FallbackLang = "ru";

    private static Dictionary<string, Dictionary<string, string>> data;

    public static string Language { get; private set; } = FallbackLang;
    public static event Action LanguageChanged;

    public static void Install(Dictionary<string, Dictionary<string, string>> strings)
    {
        data = strings;
    }

    public static void SetLanguage(string lang)
    {
        if (Language == lang) return;

        Language = lang;
        LanguageChanged?.Invoke();
    }

    public static string Get(string key)
    {
        if (data == null)
        {
            Log.Error("Loc.Install() не был вызван");
            return $"#{key}#";
        }

        if (data.TryGetValue(Language, out var table) && table.TryGetValue(key, out var value))
            return value;

        if (data.TryGetValue(FallbackLang, out var fallback) && fallback.TryGetValue(key, out var fb))
            return fb;

        Log.Warn($"Отсутствует строка: {key}");
        return $"#{key}#";
    }

    public static string FormatTime(GameTime time)
    {
        var dayPart = Get("ui.day").Replace("{0}", time.Day.ToString());
        return $"{dayPart} · {time.Hour:D2}:{time.Minute:D2}";
    }

    public static string T(string key) => Get(key);
}