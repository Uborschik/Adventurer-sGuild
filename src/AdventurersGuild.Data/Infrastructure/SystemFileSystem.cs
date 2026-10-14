namespace AdventurersGuild.Data.IO;

using System;
using System.IO;

public sealed class SystemFileSystem : IFileSystem
{
    private readonly string root;

    public SystemFileSystem()
    {
        root = AppContext.BaseDirectory;
    }

    public bool Exists(string relativePath)
        => File.Exists(Path.Combine(root, relativePath));

    public string ReadAllText(string relativePath)
        => File.ReadAllText(Path.Combine(root, relativePath));

    public void WriteAllText(string relativePath, string content)
    {
        string fullPath = Path.Combine(root, relativePath);
        string dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(fullPath, content);
    }
}