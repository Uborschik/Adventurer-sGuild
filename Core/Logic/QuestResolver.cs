using System;
using System.Collections.Generic;

public class QuestResolver
{
    private const double MarginSuccess = 0;

    private static double MarginTriumph => QuestBalance.Active.MarginTriumph;
    private static double MarginFailure => QuestBalance.Active.MarginFailure;
    private static double TriumphCapPercent => QuestBalance.Active.TriumphCapPercent;

    private readonly Random resolveRng;

    public QuestResolver(int? seed = null)
    {
        resolveRng = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public QuestResult Resolve(QuestModel quest)
    {
        var party = quest.Party ?? new List<AdventurerModel>();

        var threshold = QuestCalculator.Threshold(
            quest.RecommendedPartySize, party, quest.Weights, quest.Tier);

        var margin = QuestCalculator.Margin(
            quest.RecommendedPartySize, party, quest.Weights, quest.Tier, resolveRng);

        var effectiveTriumph = EffectiveMarginTriumph(threshold);
        var role = RoleFromMargin(margin, effectiveTriumph);

        var grade = QuestDatabase.GetByRole(role)
            ?? throw new InvalidOperationException($"Нет градации для роли {role}");

        var gold = (int)(quest.GoldReward * grade.GoldMultiplier);
        var glory = (int)(quest.GloryReward * grade.GloryMultiplier);

        return new QuestResult(role, gold, glory, "...");
    }

    public QuestPrediction Predict(QuestModel quest, IReadOnlyList<AdventurerModel> party)
    {
        var prediction = new QuestPrediction();

        if (quest == null || party == null || party.Count == 0)
        {
            foreach (QuestGradeRole role in Enum.GetValues<QuestGradeRole>())
                prediction.Set(role, 0f);
            return prediction;
        }

        var threshold = QuestCalculator.Threshold(quest.RecommendedPartySize, party, quest.Weights, quest.Tier);
        var effectiveTriumph = EffectiveMarginTriumph(threshold);

        double PAbove(double x)
        {
            double needed = threshold + x;
            int minRoll = (int)Math.Ceiling(needed);
            if (minRoll <= 1) return 1.0;
            if (minRoll > 100) return 0.0;
            return (101 - minRoll) / 100.0;
        }

        var pTriumph = PAbove(effectiveTriumph);
        var pSuccess = PAbove(MarginSuccess) - pTriumph;
        var pFailure = PAbove(MarginFailure) - PAbove(MarginSuccess);
        var pDisaster = 1.0 - PAbove(MarginFailure);

        SetGrade(prediction, QuestGradeRole.Triumph, pTriumph);
        SetGrade(prediction, QuestGradeRole.Success, pSuccess);
        SetGrade(prediction, QuestGradeRole.Failure, pFailure);
        SetGrade(prediction, QuestGradeRole.Disaster, pDisaster);

        return prediction;
    }

    private static void SetGrade(QuestPrediction prediction, QuestGradeRole role, double probability)
    {
        var info = QuestDatabase.GetByRole(role);

        if (info == null) return;

        prediction.Set(role, (float)Math.Max(0, probability * 100.0));
    }

    private static double EffectiveMarginTriumph(double threshold)
    {
        double floorValue = 101.0 - TriumphCapPercent - threshold;
        return Math.Max(MarginTriumph, floorValue);
    }

    private static QuestGradeRole RoleFromMargin(double margin, double effectiveTriumph)
    {
        if (margin >= effectiveTriumph) return QuestGradeRole.Triumph;
        if (margin >= MarginSuccess) return QuestGradeRole.Success;
        if (margin >= MarginFailure) return QuestGradeRole.Failure;
        return QuestGradeRole.Disaster;
    }
}