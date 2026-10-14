using System;

namespace AdventurersGuild.Core;

public static class Log
{
    public static Action<string> InfoSink = Console.WriteLine;
    public static Action<string> WarnSink = s => Console.WriteLine("[WARN] " + s);
    public static Action<string> ErrorSink = s => Console.WriteLine("[ERR ] " + s);

    public static void Info(string msg) => InfoSink?.Invoke(msg);
    public static void Warn(string msg) => WarnSink?.Invoke(msg);
    public static void Error(string msg) => ErrorSink?.Invoke(msg);
}