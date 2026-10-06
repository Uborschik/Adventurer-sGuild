public readonly struct QuestResult
{
    public readonly QuestGradeRole Role;
    public readonly int GoldEarned;
    public readonly int GloryEarned;
    public readonly int ExpTotal;
    public readonly bool FailedByCritical;
    public readonly string Narrative;

    public QuestResult(QuestGradeRole role, int goldEarned, int gloryEarned, int expTotal, bool failedByCritical, string narrative)
    {
        Role = role;
        GoldEarned = goldEarned;
        GloryEarned = gloryEarned;
        ExpTotal = expTotal;
        FailedByCritical = failedByCritical;
        Narrative = narrative;
    }
}