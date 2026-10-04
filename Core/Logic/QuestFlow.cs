using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class QuestFlow
{
    private const int BoardCapacity = 4;

    private readonly QuestRegistry registry;
    private readonly QuestFactory factory;
    private readonly QuestResolver resolver;
    private readonly GameClock clock;
    private readonly GuildBank bank;

    public event Action<QuestModel> Added;
    public event Action<QuestModel> Expired;
    public event Action<QuestModel> Started;
    public event Action<QuestModel> Completed;

    private readonly Random woundRng;

    public QuestFlow(QuestRegistry registry, QuestFactory factory, QuestResolver resolver, GameClock clock, GuildBank bank, int? seed = null)
    {
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        this.bank = bank ?? throw new ArgumentNullException(nameof(bank));

        woundRng = seed.HasValue ? new Random(unchecked(seed.Value * 7 + 3)) : new Random();
    }

    public void Start()
    {
        clock.TimeAdvanced += OnTimeAdvanced;
        RefreshBoard();
    }

    public void Stop()
    {
        clock.TimeAdvanced -= OnTimeAdvanced;
    }

    // === Внешний API ===

    public bool StartQuest(QuestModel quest, IReadOnlyList<AdventurerModel> party)
    {
        if (quest == null) return false;
        if (quest.Status != QuestStatus.Available) return false;
        if (party == null || party.Count == 0) return false;

        quest.Status = QuestStatus.InProgress;
        quest.Party = new List<AdventurerModel>(party);
        quest.StartTime = clock.Now;
        quest.EndTime = clock.Now + GameTime.FromMinutes(quest.DurationMinutes);

        foreach (var a in quest.Party)
            a.SetStatus(StatusIds.OnQuest);

        Started?.Invoke(quest);
        return true;
    }

    // === Обработка времени ===

    private void OnTimeAdvanced(GameTime now)
    {
        ExpireOld(now);
        CompleteReady(now);
        RefreshBoard();
    }

    private void ExpireOld(GameTime now)
    {
        foreach (var quest in registry.Available.ToList())
        {
            if (now < quest.ExpiresAt) continue;

            quest.Status = QuestStatus.Expired;
            Expired?.Invoke(quest);
        }
    }

    private void CompleteReady(GameTime now)
    {
        foreach (var quest in registry.InProgress.ToList())
        {
            if (now < quest.EndTime) continue;

            quest.Result = resolver.Resolve(quest);
            quest.Status = QuestStatus.Completed;

            var outcome = QuestDatabase.GetOutcome(quest.Result.Role);

            foreach (var a in quest.Party)
            {
                // Смерть
                if (Roll(outcome?.DeathChance ?? 0))
                {
                    var deadStatus = AdventurerDatabase.GetStatus(StatusIds.Dead);
                    var days = deadStatus?.AutoRemoveAfterDays ?? 4;
                    a.SetStatus(StatusIds.Dead, now + GameTime.FromDays(days));
                    continue;
                }

                var picked = PickStatus(outcome, now);

                if (picked == null)
                    a.SetStatus(StatusIds.Free);
                else
                    a.SetStatus(picked.Value.Id, picked.Value.ExpiresAt);
            }

            // === Начисление опыта ===
            double pool = QuestCalculator.TotalExpPool(
                quest.Level, quest.Tier, quest.Result.Role);

            if (pool > 0 && quest.Party.Count > 0)
            {
                var shares = QuestCalculator.ContributionShares(quest.Party, quest.Weights);

                foreach (var a in quest.Party)
                {
                    if (!shares.TryGetValue(a, out var share)) continue;

                    int xp = (int)Math.Round(pool * share);
                    if (xp <= 0) continue;

                    AdventurerProgress.AddExperience(a, xp);
                }
            }

            bank.AddGold(quest.Result.GoldEarned);
            bank.AddGlory(quest.Result.GloryEarned);

            Completed?.Invoke(quest);
        }
    }

    private struct PickedStatus
    {
        public string Id;
        public GameTime? ExpiresAt;
    }

    private PickedStatus? PickStatus(QuestOutcomeInfo outcome, GameTime now)
    {
        if (outcome?.Statuses == null || outcome.Statuses.Count == 0)
            return null;

        foreach (var s in outcome.Statuses)
        {
            if (!Roll(s.Chance)) continue;

            int days = s.DurationDaysMax > s.DurationDaysMin
                ? woundRng.Next(s.DurationDaysMin, s.DurationDaysMax + 1)
                : s.DurationDaysMin;

            return new PickedStatus
            {
                Id = s.Id,
                ExpiresAt = now + GameTime.FromDays(days),
            };
        }

        return null;
    }

    private bool Roll(int percent) => percent > 0 && woundRng.Next(100) < percent;

    private GameTime? RollWound(QuestGradeRole role)
    {
        switch (role)
        {
            case QuestGradeRole.Disaster:
                return GameTime.FromDays(woundRng.Next(4, 8));
            case QuestGradeRole.Failure:
                if (woundRng.Next(2) == 0)
                    return GameTime.FromDays(woundRng.Next(1, 4));
                return null;
            default:
                return null;
        }
    }

    private void RefreshBoard()
    {
        var available = registry.CountAvailable();

        while (available < BoardCapacity)
        {
            var quest = factory.Create(clock.Now);

            if (quest == null) break;

            registry.Add(quest);
            Added?.Invoke(quest);
            available++;
        }
    }
}