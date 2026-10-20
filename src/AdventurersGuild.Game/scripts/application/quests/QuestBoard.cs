namespace AdventurersGuild.Application.Quests;

using System.Linq;
using AdventurersGuild.Core;
using AdventurersGuild.Domain.Game;
using AdventurersGuild.Domain.Quests;

public class QuestBoard(QuestRegistry registry, QuestFactory factory)
{
    public void Refresh(GameTime now)
    {
        while (registry.CountAvailable() < 4)
        {
            var lvl = GameRules.InitialQuestLvl;
            var q = factory.Create(now, lvl, 4);

            if (q != null) registry.Add(q);
            else break;
        }
    }

    public void Expire(GameTime now)
    {
        foreach (var quest in registry.Board.ToList())
        {
            if (now >= quest.ExpiresAt)
            {
                quest.Status = QuestStatus.Expired;
                registry.ExpireToHistory(quest);
            }
        }
    }
}