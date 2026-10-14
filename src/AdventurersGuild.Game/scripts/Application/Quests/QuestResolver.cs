using System;
using System.Collections.Generic;
using System.Linq;
using AdventurersGuild.Data.Quest;
using AdventurersGuild.Domain.Adventurers;
using AdventurersGuild.Domain.Quests;

namespace AdventurersGuild.Application.Quests;

public class QuestResolver
{
    public QuestResolver(int? seed = null) { }

    public QuestResult Resolve(QuestModel quest)
    {
        var phaseResults = quest.PhaseResults ?? new List<PhaseResult>();

        var role = QuestCalculator.DetermineRole(phaseResults);
        bool failed = QuestCalculator.IsQuestFailed(phaseResults);

        int gold = failed ? 0 : quest.GoldReward;
        int glory = failed ? 0 : quest.GloryReward;
        int exp = QuestCalculator.TotalExp(phaseResults);

        return new QuestResult(role, gold, glory, exp, failed, "...");
    }

    public QuestPrediction Predict(QuestModel quest, IReadOnlyList<AdventurerModel> party)
    {
        var prediction = new QuestPrediction();

        if (quest == null || party == null || party.Count == 0)
        {
            prediction.Role = QuestGradeRole.Disaster;
            prediction.PhasesTotal = quest?.Phases?.Count ?? 0;
            return prediction;
        }

        var results = new List<PhaseResult>();

        foreach (var phase in quest.Phases)
        {
            if (phase.PhaseId == "short_rest" || phase.PhaseId == "long_rest") continue;

            var rolls = QuestCalculator.ExpectedRolls(
                phase, party, quest.CodeLevel);

            bool expectedPass = rolls.Count > 0 && rolls.All(r => r.Chance >= 50);

            results.Add(new PhaseResult
            {
                Phase = phase,
                Passed = expectedPass,
                EffectiveCritical = phase.Critical,
                ExpEarned = expectedPass ? phase.ExpReward : 0,
                DayCompleted = 0,
                Attempts = 1,
                Rolls = rolls,
            });
        }

        prediction.Role = QuestCalculator.DetermineRole(results);
        prediction.PhasesPassed = results.Count(r => r.Passed);
        prediction.PhasesTotal = results.Count;
        prediction.CriticalPassed = results.Count(r => r.EffectiveCritical && r.Passed);
        prediction.CriticalTotal = results.Count(r => r.EffectiveCritical);
        prediction.ExpTotal = QuestCalculator.TotalExp(results);
        prediction.IsFailed = QuestCalculator.IsQuestFailed(results);
        prediction.PhasePredictions = results.Select(r => new PhasePrediction
        {
            PhaseId = r.Phase.PhaseId,
            ExpectedPass = r.Passed,
            MinChance = r.Rolls != null && r.Rolls.Count > 0
        ? r.Rolls.Min(x => x.Chance)
        : 0,
            Rolls = r.Rolls,
        }).ToList();

        return prediction;
    }
}