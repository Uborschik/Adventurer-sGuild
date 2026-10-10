namespace AdventurersGuild.Application.Quests;

using AdventurersGuild.Domain.Quests;

public readonly record struct AmbientCandidate(CreatureInfo Creature, int Weight);