namespace AdventurersGuild.Domain.Quests;

using System.Collections.Generic;

public class QuestBalanceProfile
{
    public string Id { get; set; } = string.Empty;

    // === Бюджет квестов ===
    public Dictionary<int, int> BudgetPerLevel { get; set; } = [];

    // === Здоровье и Бой ===
    public int HpBase { get; set; } = 10;
    public int HpPerEndurance { get; set; } = 5;

    // === Раны (сколько дней лечатся) ===
    public int LightWoundDays { get; set; } = 1;
    public int MediumWoundDays { get; set; } = 3;
    public int HeavyWoundDays { get; set; } = 5;

    // === Лимиты ===
    public int MaxQuestDays { get; set; } = 90;
}

public class QuestBalanceDatabase
{
    public List<QuestBalanceProfile> Profiles { get; set; } = [];
}