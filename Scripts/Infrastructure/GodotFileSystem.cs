using System;
using AdventurersGuild.Data.IO;
using Godot;

namespace AdventurersGuild.Infrastructure;

public sealed class GodotFileSystem : IFileSystem
{
    public bool Exists(string path) => FileAccess.FileExists(path);

    public string ReadAllText(string path) => FileAccess.GetFileAsString(path);

    public void WriteAllText(string path, string content)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
            throw new InvalidOperationException(
                $"GodotFileSystem: не удалось открыть '{path}' для записи " +
                $"({FileAccess.GetOpenError()})");

        file.StoreString(content);
    }
}