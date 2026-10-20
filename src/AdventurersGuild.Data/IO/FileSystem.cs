using System;

namespace AdventurersGuild.Data.IO;

public static class FileSystem
{
    public static IFileSystem Current { get; set; }

    public static void EnsureReady()
    {
        if (Current == null)
            throw new InvalidOperationException(
                "FileSystem.Current не установлен. " +
                "Зарегистрируй реализацию IFileSystem на старте приложения.");
    }
}