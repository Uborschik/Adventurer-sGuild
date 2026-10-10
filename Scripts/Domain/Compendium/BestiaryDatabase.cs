namespace AdventurersGuild.Domain.Compendium;

using System.Collections.Generic;

public class BestiaryDatabase
{
    public List<HabitatEntry> Habitats { get; set; } = new();
}

public class HabitatEntry
{
    public string LocationId { get; set; }
    public List<BestiaryCreature> Creatures { get; set; } = new();
}

public readonly record struct BestiaryCreature(string CreatureId, int Weight)
{
    public int EffectiveWeight => Weight > 0 ? Weight : 10;
}