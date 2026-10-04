using System.Collections.Generic;
using System.Linq;

public class QuestRegistry
{
    private readonly List<QuestModel> all = new();

    public IReadOnlyList<QuestModel> All => all;

    public IEnumerable<QuestModel> Available => all.Where(q => q.Status == QuestStatus.Available);
    public IEnumerable<QuestModel> InProgress => all.Where(q => q.Status == QuestStatus.InProgress);
    public IEnumerable<QuestModel> Completed => all.Where(q => q.Status == QuestStatus.Completed);
    public IEnumerable<QuestModel> Expired => all.Where(q => q.Status == QuestStatus.Expired);

    public void Add(QuestModel quest) => all.Add(quest);

    public QuestModel GetById(string id)
    {
        foreach (var q in all) if (q.Id == id) return q;
        return null;
    }

    public int CountAvailable()
    {
        int count = 0;
        for (int i = 0; i < all.Count; i++)
            if (all[i].Status == QuestStatus.Available) count++;
        return count;
    }

    public bool Remove(string id)
    {
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].Id == id)
            {
                all.RemoveAt(i);
                return true;
            }
        }
        return false;
    }
}