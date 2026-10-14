using System.Collections.Generic;
using Godot;

namespace AdventurersGuild.Infrastructure;

public static class IconLoader
{
    private const string IconPath = "res://Resources/Sprites/Icons/";

    private static readonly Dictionary<string, Texture2D> cache = new();

    public static Texture2D Get(string category, string id)
    {
        if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(id))
            return null;

        string key = $"{category}/{id}";
        if (cache.TryGetValue(key, out var cached)) return cached;

        var tex = Load(category, id);
        cache[key] = tex;
        return tex;
    }

    private static Texture2D Load(string category, string id)
    {
        string path = $"{IconPath}{category}/{id}.tres";
        if (ResourceLoader.Exists(path))
            return GD.Load<Texture2D>(path);

        string fallback = $"{IconPath}{category}/_default.tres";
        if (ResourceLoader.Exists(fallback))
            return GD.Load<Texture2D>(fallback);

        GD.PushWarning($"[Icons] Нет иконки: {category}/{id} (и нет _default)");
        return null;
    }
}