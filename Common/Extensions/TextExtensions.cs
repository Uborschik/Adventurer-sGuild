public static class TextExtensions
{
    public static string ClassName(this AdventurerModel model)
        => AdventurerDatabase.ClassName(model.ClassId);

    public static string RaceName(this AdventurerModel model)
        => AdventurerDatabase.RaceName(model.RaceId);

    public static string StatName(this AdventurerModel model, string id)
        => AdventurerDatabase.StatName(id);

    public static double StatValue(this AdventurerModel model, string id)
        => model.Stats.TryGetValue(id, out var v) ? v : 0;

    public static string StatLine(this AdventurerModel model, string id)
        => $"{model.StatName(id)}: {model.StatValue(id):F0}";

    public static string StatusName(this AdventurerModel model, string id)
        => AdventurerDatabase.StatusName(id);
}