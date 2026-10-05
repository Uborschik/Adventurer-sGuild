public class QuestPhase(QuestPhaseTemplate template, int dc)
{
    public QuestPhaseTemplate Template { get; } = template;
    public int DC { get; } = dc;
}