namespace AdventurersGuild.Data.Compendium;

using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Core;
using AdventurersGuild.Data.Quest;

public static partial class CompendiumDatabase
{
    private static void Validate()
    {
        int errors = 0;

        if (Bestiary == null || Bestiary.Habitats == null || Bestiary.Habitats.Count == 0)
        {
            Log.Error("[CompendiumDB] Bestiary пустой");
            Log.Error($"[CompendiumDB] Валидация: 1 ошибок");
            return;
        }

        var coveredLocations = new HashSet<string>();
        var coveredCreatures = new HashSet<string>();

        foreach (var habitat in Bestiary.Habitats)
        {
            if (string.IsNullOrEmpty(habitat.LocationId))
            {
                Log.Error("[CompendiumDB] Habitat без locationId");
                errors++;
                continue;
            }

            if (!QuestDatabase.Locations.ContainsKey(habitat.LocationId))
            {
                Log.Error($"[CompendiumDB] Habitat: неизвестная локация '{habitat.LocationId}'");
                errors++;
            }
            else
            {
                coveredLocations.Add(habitat.LocationId);
            }

            if (habitat.Creatures == null || habitat.Creatures.Count == 0)
            {
                Log.Error($"[CompendiumDB] Habitat '{habitat.LocationId}': нет существ");
                errors++;
                continue;
            }

            var seen = new HashSet<string>();
            foreach (var c in habitat.Creatures)
            {
                if (string.IsNullOrEmpty(c.CreatureId))
                {
                    Log.Error($"[CompendiumDB] Habitat '{habitat.LocationId}': существо без id");
                    errors++;
                    continue;
                }

                if (!seen.Add(c.CreatureId))
                {
                    Log.Error($"[CompendiumDB] Habitat '{habitat.LocationId}': " +
                              $"дубликат существа '{c.CreatureId}'");
                    errors++;
                }

                if (!QuestDatabase.Creatures.ContainsKey(c.CreatureId))
                {
                    Log.Error($"[CompendiumDB] Habitat '{habitat.LocationId}': " +
                              $"неизвестное существо '{c.CreatureId}'");
                    errors++;
                }
                else
                {
                    coveredCreatures.Add(c.CreatureId);
                }

                if (c.Weight <= 0)
                {
                    Log.Error($"[CompendiumDB] Habitat '{habitat.LocationId}': " +
                              $"вес существа '{c.CreatureId}' <= 0");
                    errors++;
                }
            }
        }

        // Все локации покрыты
        foreach (var id in QuestDatabase.Locations.Keys)
        {
            if (!coveredLocations.Contains(id))
            {
                Log.Error($"[CompendiumDB] Локация '{id}' не присутствует в Bestiary");
                errors++;
            }
        }

        // Все существа покрыты
        foreach (var id in QuestDatabase.Creatures.Keys)
        {
            if (!coveredCreatures.Contains(id))
            {
                Log.Error($"[CompendiumDB] Существо '{id}' не присутствует ни в одной локации");
                errors++;
            }
        }

        if (errors == 0)
            Log.Info($"[CompendiumDB] Валидация: ok " +
                     $"({Bestiary.Habitats.Count} локаций, " +
                     $"{coveredCreatures.Count} существ покрыто)");
        else
            Log.Error($"[CompendiumDB] Валидация: {errors} ошибок");
    }
}