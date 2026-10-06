using System.Collections.Generic;

public class CreatureInfo
{
    public string Id { get; set; }
    public LocalizedNoun Name { get; set; }
    public string Tier { get; set; }
    public CreaturePhaseEffects PhaseEffects { get; set; }
}

public class CreaturePhaseEffects
{
    public List<AddedPhase> Adds { get; set; }
    public Dictionary<string, PhaseModifier> Modifies { get; set; }
}

public class AddedPhase
{
    public string Phase { get; set; }
    public string Before { get; set; }
    public List<PhaseSolution> Solutions { get; set; } = new();
}

public class PhaseModifier
{
    public double? DcDelta { get; set; }
    public int? DurationDeltaMinutes { get; set; }
    public Dictionary<string, StatDcDelta> StatModifiers { get; set; }
}

public class StatDcDelta
{
    public double DcDelta { get; set; }
}

public class CreatureDatabase
{
    public List<CreatureInfo> Creatures { get; set; } = new();
}