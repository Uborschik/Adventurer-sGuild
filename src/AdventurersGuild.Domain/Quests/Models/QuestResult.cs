namespace AdventurersGuild.Domain.Quests;

public readonly struct QuestResult(QuestGradeRole role, int goldEarned, int gloryEarned, int expTotal, bool failedByCritical, string narrative)
{
    public readonly QuestGradeRole Role = role;
    public readonly int GoldEarned = goldEarned;
    public readonly int GloryEarned = gloryEarned;
    public readonly int ExpTotal = expTotal;
    public readonly bool FailedByCritical = failedByCritical;
    public readonly string Narrative = narrative;
}