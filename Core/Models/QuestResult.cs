public readonly struct QuestResult
{
    public readonly QuestGradeRole Role;
    public readonly int GoldEarned;
    public readonly int GloryEarned;
    public readonly string Narrative;

    public QuestResult(QuestGradeRole role, int goldEarned, int gloryEarned, string narrative)
    {
        Role = role;
        GoldEarned = goldEarned;
        GloryEarned = gloryEarned;
        Narrative = narrative;
    }
}