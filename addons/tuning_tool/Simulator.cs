using System;
using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Application.Quests;
using AdventurersGuild.Data.Quest;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Quests;

public static class Simulator
{
    public static SimulationResult Run(QuestModel quest, IReadOnlyList<AdventurerModel> party, int trials, int? seed = null)
    {
        if (quest == null) throw new ArgumentNullException(nameof(quest));

        var result = new SimulationResult { Trials = trials };
        if (party == null || party.Count == 0 || trials <= 0)
            return result;

        var rng = seed.HasValue ? new Random(seed.Value) : new Random();

        int phasesPassedSum = 0;
        int expSum = 0;

        for (int t = 0; t < trials; t++)
        {
            var copy = DeepCopyParty(party);
            var phaseResults = SimulateOne(quest, copy, rng);

            switch (QuestCalculator.DetermineRole(phaseResults))
            {
                case QuestGradeRole.Triumph: result.TriumphCount++; break;
                case QuestGradeRole.Success: result.SuccessCount++; break;
                case QuestGradeRole.Failure: result.FailureCount++; break;
                case QuestGradeRole.Disaster: result.DisasterCount++; break;
            }

            phasesPassedSum += phaseResults.Count(r => r.Passed);
            expSum += QuestCalculator.TotalExp(phaseResults);
        }

        result.AvgPhasesPassed = (double)phasesPassedSum / trials;
        result.AvgExpEarned = (double)expSum / trials;

        return result;
    }

    private static List<PhaseResult> SimulateOne(QuestModel quest, IReadOnlyList<AdventurerModel> party, Random rng)
    {
        var results = new List<PhaseResult>();

        foreach (var phase in quest.Phases)
        {
            if (phase.Id == "short_rest" || phase.Id == "long_rest")
                continue;

            var outcome = QuestCalculator.EvaluatePhase(
                phase, party, quest.CodeLevel, rng);

            results.Add(new PhaseResult
            {
                Phase = phase,
                Passed = outcome.Passed,
                EffectiveCritical = phase.Critical,
                ExpEarned = outcome.Passed ? phase.ExpReward : 0,
                DayCompleted = 0,
                Attempts = 1,
                Rolls = outcome.Rolls,
            });

            if (phase.Critical && !outcome.Passed)
                break;
        }

        return results;
    }

    private static List<AdventurerModel> DeepCopyParty(
        IReadOnlyList<AdventurerModel> party)
    {
        var copy = new List<AdventurerModel>(party.Count);

        foreach (var a in party)
        {
            var statsCopy = a.Stats != null
                ? new Dictionary<string, double>(a.Stats)
                : new Dictionary<string, double>();

            copy.Add(new AdventurerModel(
                id: a.Id,
                firstName: a.FirstName,
                lastName: a.LastName,
                classId: a.ClassId,
                raceId: a.RaceId,
                stats: statsCopy,
                level: a.Level.Number));
        }

        return copy;
    }
}