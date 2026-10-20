namespace AdventurersGuild.Application.Quests;

using System.Linq;
using AdventurersGuild.Domain.Quests;

public class QuestResolver
{
    public QuestResult Resolve(QuestModel quest)
    {
        int passed = quest.Results.Count(r => r.Passed);
        int total = quest.Encounters.Count;

        var mainResult = quest.Results.FirstOrDefault(r => r.Encounter.IsMainEncounter);
        bool mainPassed = mainResult == null || mainResult.Passed;
        bool failed = !mainPassed;

        QuestGradeRole role;
        if (failed) role = QuestGradeRole.Disaster;
        else if (passed == total) role = QuestGradeRole.Triumph;
        else if (passed >= total / 2.0) role = QuestGradeRole.Success;
        else role = QuestGradeRole.Failure;

        return new QuestResult(role,
            failed ? 0 : quest.GoldReward,
            failed ? 0 : quest.GloryReward,
            0, // Exp посчитаем позже
            failed, "...");
    }
}