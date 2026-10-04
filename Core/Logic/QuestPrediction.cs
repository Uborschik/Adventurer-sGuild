using System.Collections.Generic;

public class QuestPrediction
{
    private readonly Dictionary<QuestGradeRole, float> probabilities = new();

    public IReadOnlyDictionary<QuestGradeRole, float> Probabilities => probabilities;

    public void Set(QuestGradeRole role, float probability) => probabilities[role] = probability;

    public float Get(QuestGradeRole role) => probabilities.TryGetValue(role, out var p) ? p : 0f;

    public void Clear() => probabilities.Clear();

    public float SuccessOrBetter
    {
        get
        {
            float sum = 0;

            foreach (var kv in probabilities)
            {
                var grade = QuestDatabase.GetByRole(kv.Key);
                if (grade != null && grade.CountsAsSuccess)
                    sum += kv.Value;
            }

            return sum;
        }
    }
}