namespace AdventurersGuild.Editor.BalanceLab;

#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdventurersGuild.Domain.Units;
using AdventurersGuild.Domain.Combat;
using AdventurersGuild.Domain.Quests;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Data.Rules;


[Tool]
public partial class CombatLabTab : MarginContainer
{
    [Export] private PackedScene cardScene;
    [Export] private PackedScene popupScene;
    [Export] private VBoxContainer partyList;
    [Export] private VBoxContainer enemyList;
    [Export] private Button addHeroBtn;
    [Export] private Button addEnemyBtn;
    [Export] private Button runBtn;
    [Export] private Button resetBtn;
    [Export] private Label resultLabel;

    private CombatLabPopup popup;
    private CombatLabCard editingCard;
    private bool editingEnemy;

    public override void _Ready()
    {
        // 1. Инициализация данных
        if (FileSystem.Current == null)
        {
            try { FileSystem.Current = new SystemFileSystem(); }
            catch (Exception ex) { GD.PrintErr($"[BalanceLab] FS init error: {ex.Message}"); }
        }

        if (UnitDatabase.Species == null || UnitDatabase.Species.Count == 0)
        {
            GameRulesLoader.Load();
            Data.Units.UnitLoader.Load();
            Data.Quests.QuestLoader.Load();
        }

        // 2. Создаем попап
        popup = popupScene.Instantiate<CombatLabPopup>();
        AddChild(popup);
        popup.Saved += OnPopupSaved;

        // 3. Подписка на кнопки
        addHeroBtn.Pressed += () => OpenPopup(null, false);
        addEnemyBtn.Pressed += () => OpenPopup(null, true);
        runBtn.Pressed += OnRunPressed;
        resetBtn.Pressed += OnResetPressed;

        // 4. Проверка: не заполнять листы, если сцена открыта для редактирования
        if (GetTree().EditedSceneRoot != this)
        {
            OnResetPressed();
        }
        else
        {
            // Если это редактор, просто очищаем листы от случайно сохраненных карточек
            foreach (Node child in partyList.GetChildren()) child.QueueFree();
            foreach (Node child in enemyList.GetChildren()) child.QueueFree();
        }
    }

    public override void _ExitTree()
    {
        // 1. Очищаем списки карточек
        foreach (Node child in partyList.GetChildren())
        {
            child.QueueFree();
        }
        foreach (Node child in enemyList.GetChildren())
        {
            child.QueueFree();
        }

        // 2. Отписываемся от ивентов попапа, чтобы не было утечек памяти
        if (popup != null)
        {
            popup.Saved -= OnPopupSaved;
        }
    }

    private void OpenPopup(CombatLabCard card, bool isEnemy)
    {
        editingCard = card;
        editingEnemy = isEnemy;
        popup.Open(card?.Data ?? new UnitConfigData(), isEnemy);
    }

    private void OnPopupSaved(UnitConfigData data)
    {
        if (editingCard != null)
        {
            // Редактируем существующую карточку
            editingCard.Data = data;
            editingCard.UpdateUI();
        }
        else
        {
            // Создаем новую карточку
            var card = cardScene.Instantiate<CombatLabCard>();
            card.Data = data;

            // Подписываемся на ивенты карточки
            card.EditRequested += OnCardEditRequested;
            card.RemoveRequested += OnCardRemoveRequested;

            if (editingEnemy)
                enemyList.AddChild(card);
            else
                partyList.AddChild(card);

            card.UpdateUI();
        }
    }

    // Обработчики событий от карточек
    private void OnCardEditRequested(CombatLabCard card)
    {
        OpenPopup(card, card.GetParent() == enemyList);
    }

    private void OnCardRemoveRequested(CombatLabCard card)
    {
        card.QueueFree();
    }

    private void OnResetPressed()
    {
        // Очищаем списки
        foreach (Node child in partyList.GetChildren()) child.QueueFree();
        foreach (Node child in enemyList.GetChildren()) child.QueueFree();

        // Создаем дефолтного героя
        editingEnemy = false;
        OnPopupSaved(new UnitConfigData
        {
            SpeciesId = "human",
            ClassId = "warrior",
            Level = 1,
            CustomStats = new Dictionary<string, double> { { "str", 10 }, { "dex", 10 }, { "end", 10 }, { "int", 10 }, { "wis", 10 }, { "cha", 10 } }
        });

        // Создаем дефолтного врага
        editingEnemy = true;
        OnPopupSaved(new UnitConfigData
        {
            SpeciesId = "wolf",
            ClassId = "young",
            Level = 1,
            CustomStats = new Dictionary<string, double> { { "str", 10 }, { "dex", 10 }, { "end", 10 }, { "int", 10 }, { "wis", 10 }, { "cha", 10 } }
        });

        resultLabel.Text = "Awaiting simulation...";
    }

    private async void OnRunPressed()
    {
        runBtn.Disabled = true;
        resultLabel.Text = "Running simulation in background...";

        var heroCards = partyList.GetChildren().OfType<CombatLabCard>().ToList();
        var enemyCards = enemyList.GetChildren().OfType<CombatLabCard>().ToList();

        if (heroCards.Count == 0 || enemyCards.Count == 0)
        {
            resultLabel.Text = "Error: Need at least 1 hero and 1 enemy.";
            runBtn.Disabled = false;
            return;
        }

        try
        {
            // Запускаем тяжелый расчет в фоне
            string resultText = await Task.Run(() => RunSimulationCore(heroCards, enemyCards));
            resultLabel.Text = resultText;
        }
        catch (Exception ex)
        {
            resultLabel.Text = $"Error: {ex.Message}";
        }
        finally
        {
            runBtn.Disabled = false;
        }
    }

    private string RunSimulationCore(List<CombatLabCard> heroCards, List<CombatLabCard> enemyCards)
    {
        int iterations = 1000;
        int wins = 0;
        int totalRounds = 0;

        // 1. Формируем группу врагов (EncounterUnit)
        var enemyUnits = enemyCards.Select(c =>
            new EncounterUnit(c.Data.SpeciesId, c.Data.ClassId, "combat", c.Data.Level)
        ).ToList();

        var encounter = new QuestEncounterInstance
        {
            Units = enemyUnits,
            IsMainEncounter = true
        };

        for (int i = 0; i < iterations; i++)
        {
            // 2. Каждую итерацию пересоздаем отряд (т.к. симулятор меняет им HP)
            var party = heroCards.Select(c =>
                new UnitModel(Guid.NewGuid().ToString(), "Hero", "Lab", c.Data.ClassId, c.Data.SpeciesId, c.Data.CustomStats, c.Data.Level)
            ).ToList();

            var quest = new QuestModel { Party = party };

            // 3. Вызываем твой оригинальный CombatSimulator
            var combatResult = CombatSimulator.Simulate(quest, encounter);

            if (combatResult.PartyWon) wins++;

            // Считаем раунды по событиям
            int rounds = combatResult.Events.Count(e => e.Type == CombatEventType.RoundStart);
            totalRounds += rounds;
        }

        double winRate = (double)wins / iterations * 100;
        double avgRounds = (double)totalRounds / iterations;

        return $"Simulation Complete ({iterations} runs):\n" +
               $"Win Rate: {winRate:F1}%\n" +
               $"Avg Rounds: {avgRounds:F2}";
    }
}
#endif