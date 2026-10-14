namespace AdventurersGuild.Data;

using System.Collections.Generic;
using AdventurersGuild.Core;
using AdventurersGuild.Data.IO;

public static class LocLoader
{
    private const string path = "resources/Data/Strings.json";

    public static void Load()
    {
        var data = JsonLoader.Load<Dictionary<string, Dictionary<string, string>>>(path);
        Loc.Install(data);
    }
}