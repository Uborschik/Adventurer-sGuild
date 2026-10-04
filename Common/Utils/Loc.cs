using System;
using System.Collections.Generic;
using Godot;

public static class Loc
{
    private const string Path = "res://Resources/Data/Strings.json";
    private const string FallbackLang = "ru";

    private static Dictionary<string, Dictionary<string, string>> data;

    public static string Language { get; private set; } = FallbackLang;
    public static event Action LanguageChanged;

    public static void Load()
    {
        if (data != null) return;

        data = JsonLoader.Load<Dictionary<string, Dictionary<string, string>>>(Path);
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
            GD.PushError("Loc.Load() не был вызван");
            return $"#{key}#";
        }

        if (data.TryGetValue(Language, out var table) && table.TryGetValue(key, out var value))
            return value;

        if (data.TryGetValue(FallbackLang, out var fallback) && fallback.TryGetValue(key, out var fb))
            return fb;

        GD.PushWarning($"Отсутствует строка: {key}");
        return $"#{key}#";
    }

    public static string FormatTime(GameTime time)
    {
        var dayPart = Loc.Get("ui.day").Replace("{0}", time.Day.ToString());
        return $"{dayPart} · {time.Hour:D2}:{time.Minute:D2}";
    }

    public static string T(string key) => Get(key);
}