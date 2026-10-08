using System.Collections.Generic;

public class QuestRegistry
{
    private readonly List<QuestModel> board = new();      // Available
    private readonly List<QuestModel> active = new();     // Pending + InProgress
    private readonly List<QuestModel> history = new();    // Completed + Expired

    public IReadOnlyList<QuestModel> Board => board;
    public IReadOnlyList<QuestModel> Active => active;
    public IReadOnlyList<QuestModel> History => history;

    // Обратная совместимость для UI и отладки
    public IEnumerable<QuestModel> All
    {
        get
        {
            foreach (var q in board) yield return q;
            foreach (var q in active) yield return q;
            foreach (var q in history) yield return q;
        }
    }

    public IEnumerable<QuestModel> Available
    {
        get
        {
            foreach (var q in board) yield return q;
        }
    }

    public IEnumerable<QuestModel> InProgress
    {
        get
        {
            foreach (var q in active) yield return q;
        }
    }

    public IEnumerable<QuestModel> Completed
    {
        get
        {
            foreach (var q in history)
                if (q.Status == QuestStatus.Completed) yield return q;
        }
    }

    public IEnumerable<QuestModel> Expired
    {
        get
        {
            foreach (var q in history)
                if (q.Status == QuestStatus.Expired) yield return q;
        }
    }

    // === Добавление ===

    public void Add(QuestModel quest)
    {
        if (quest == null) return;

        switch (quest.Status)
        {
            case QuestStatus.Available:
                board.Add(quest);
                break;
            case QuestStatus.Pending:
            case QuestStatus.InProgress:
                active.Add(quest);
                break;
            case QuestStatus.Completed:
            case QuestStatus.Expired:
                history.Add(quest);
                break;
        }
    }

    // === Переходы ===

    public void TakeFromBoard(QuestModel quest)
    {
        if (quest == null) return;
        board.Remove(quest);
        active.Add(quest);
    }

    public void CompleteToHistory(QuestModel quest)
    {
        if (quest == null) return;
        active.Remove(quest);
        history.Add(quest);
    }

    public void ExpireToHistory(QuestModel quest)
    {
        if (quest == null) return;
        board.Remove(quest);
        history.Add(quest);
    }

    // === Поиск ===

    public QuestModel GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        for (int i = 0; i < board.Count; i++)
            if (board[i].Id == id) return board[i];

        for (int i = 0; i < active.Count; i++)
            if (active[i].Id == id) return active[i];

        for (int i = 0; i < history.Count; i++)
            if (history[i].Id == id) return history[i];

        return null;
    }

    // === Счётчики ===

    public int CountAvailable() => board.Count;
    public int CountActive() => active.Count;
    public int CountHistory() => history.Count;

    // === Удаление ===

    public bool Remove(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;

        for (int i = 0; i < board.Count; i++)
            if (board[i].Id == id) { board.RemoveAt(i); return true; }

        for (int i = 0; i < active.Count; i++)
            if (active[i].Id == id) { active.RemoveAt(i); return true; }

        for (int i = 0; i < history.Count; i++)
            if (history[i].Id == id) { history.RemoveAt(i); return true; }

        return false;
    }

    // === Очистка истории ===

    public int TrimHistory(GameTime now, int maxAgeDays)
    {
        int removed = 0;
        var cutoff = now - GameTime.FromDays(maxAgeDays);

        for (int i = history.Count - 1; i >= 0; i--)
        {
            var q = history[i];
            var finishedAt = q.Status == QuestStatus.Completed
                ? q.LastProcessedAt
                : q.ExpiresAt;

            if (finishedAt < cutoff)
            {
                history.RemoveAt(i);
                removed++;
            }
        }

        return removed;
    }
}