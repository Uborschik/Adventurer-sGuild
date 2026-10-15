using AdventurersGuild.Data.Adventurer;
using AdventurersGuild.Domain.Adventurers;

namespace AdventurersGuild.Infrastructure;

public static class TextExtensions
{
    public static string ClassName(this UnitModel model)
        => UnitDatabase.ClassName(model.ClassId);

    public static string RaceName(this UnitModel model)
        => UnitDatabase.RaceName(model.RaceId);

    public static string StatName(this UnitModel model, string id)
        => UnitDatabase.StatName(id);

    public static double StatValue(this UnitModel model, string id)
        => model.Stats.TryGetValue(id, out var v) ? v : 0;

    public static string StatLine(this UnitModel model, string id)
        => $"{model.StatName(id)}: {model.StatValue(id):F0}";

    public static string StatusName(this UnitModel model, string id)
        => UnitDatabase.StatusName(id);
}