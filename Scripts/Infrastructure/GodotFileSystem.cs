namespace AdventurersGuild.Infrastructure;

using System;
using AdventurersGuild.Data.IO;
using Godot;

public sealed class GodotFileSystem : IFileSystem
{
    private const string Root = "res://";

    public bool Exists(string relativePath) => FileAccess.FileExists(Root + relativePath);

    public string ReadAllText(string relativePath) => FileAccess.GetFileAsString(Root + relativePath);

    public void WriteAllText(string relativePath, string content)
    {
        using var file = FileAccess.Open(Root + relativePath, FileAccess.ModeFlags.Write);
        if (file == null)
            throw new InvalidOperationException(
                $"GodotFileSystem: не удалось открыть '{relativePath}' для записи " +
                $"({FileAccess.GetOpenError()})");

        file.StoreString(content);
    }
}