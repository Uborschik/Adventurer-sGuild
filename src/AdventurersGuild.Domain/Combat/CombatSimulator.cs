namespace AdventurersGuild.Domain.Combat;

using System;
using System.Collections.Generic;
using AdventurersGuild.Domain.Quests;
using AdventurersGuild.Domain.Units;

public class CombatSimulator
{
    public static CombatResult Simulate(QuestModel quest, QuestEncounterInstance encounter)
    {
        var result = new CombatResult();
        var rules = LevelBalance.Active.Combat;

        // 1. Создаем бойцов отряда
        var party = new List<CombatActor>(quest.Party.Count);
        for (int i = 0; i < quest.Party.Count; i++)
        {
            var adv = quest.Party[i];
            if (adv.HasStatus(StatusIds.Dead) || adv.CurrentHp <= 0) continue;
            party.Add(CombatActorFactory.FromAdventurer(adv));
        }

        if (party.Count == 0)
        {
            result.PartyWon = false;
            result.Events.Add(CombatEvent.NoParty());
            return result;
        }

        // 2. Создаем врага
        var enemy = CombatActorFactory.FromEnemy(encounter);

        // Событие старта боя (показывает статы врага)
        result.Events.Add(CombatEvent.Start(enemy.Name, enemy.CurrentHp, enemy.MaxHp));

        // 3. Сортируем по инициативе
        var turnOrder = new List<CombatActor>(party.Count + 1);
        turnOrder.AddRange(party);
        turnOrder.Add(enemy);
        turnOrder.Sort((a, b) => b.Initiative.CompareTo(a.Initiative));

        // 4. Цикл боя
        int round = 1;
        while (round <= rules.MaxRounds && enemy.IsAlive && HasAlivePartyMembers(party))
        {
            result.Events.Add(CombatEvent.RoundStart(round));

            for (int i = 0; i < turnOrder.Count; i++)
            {
                var actor = turnOrder[i];
                if (!actor.IsAlive) continue;
                if (!enemy.IsAlive || !HasAlivePartyMembers(party)) break;

                if (actor.IsEnemy)
                {
                    EnemyTurn(enemy, party, result, round);
                }
                else
                {
                    var attackRes = ExecuteAttack(actor, enemy);
                    result.Events.Add(CombatEvent.Attack(
                        round, actor.Name, enemy.Name, attackRes.Roll, attackRes.Total,
                        enemy.Ac, attackRes.Damage, enemy.CurrentHp, enemy.MaxHp, attackRes.Hit));

                    if (!enemy.IsAlive)
                        result.Events.Add(CombatEvent.Death(round, enemy.Name));
                }
            }
            round++;
        }

        // 5. Итоги боя
        if (!enemy.IsAlive)
        {
            result.PartyWon = true;
            result.Events.Add(CombatEvent.Victory(round, enemy.Name));
        }
        else if (!HasAlivePartyMembers(party))
        {
            result.PartyWon = false;
            result.Events.Add(CombatEvent.Defeat(round));
        }
        else
        {
            result.PartyWon = false;
            result.Events.Add(CombatEvent.Timeout(round));
        }

        // 6. Синхронизация HP и ран
        for (int i = 0; i < party.Count; i++)
        {
            var actor = party[i];
            if (actor.OriginalAdventurer != null)
            {
                actor.OriginalAdventurer.SetCurrentHp(actor.CurrentHp);
                result.AdventurerWounds[actor.OriginalAdventurer.Id] =
                    CombatMath.GetWoundLevel(actor.CurrentHp, actor.MaxHp);
            }
        }

        return result;
    }

    private static bool HasAlivePartyMembers(List<CombatActor> party)
    {
        for (int i = 0; i < party.Count; i++)
            if (party[i].IsAlive) return true;
        return false;
    }

    private static void EnemyTurn(CombatActor enemy, List<CombatActor> party, CombatResult result, int round)
    {
        int aliveCount = 0;
        for (int i = 0; i < party.Count; i++)
            if (party[i].IsAlive) aliveCount++;

        if (aliveCount == 0) return;

        int targetIndex = Random.Shared.Next(aliveCount);
        int current = 0;
        CombatActor target = null;

        for (int i = 0; i < party.Count; i++)
        {
            if (party[i].IsAlive)
            {
                if (current == targetIndex) { target = party[i]; break; }
                current++;
            }
        }

        if (target == null) return;

        var attackRes = ExecuteAttack(enemy, target);

        result.Events.Add(CombatEvent.Attack(
            round, enemy.Name, target.Name, attackRes.Roll, attackRes.Total,
            target.Ac, attackRes.Damage, target.CurrentHp, target.MaxHp, attackRes.Hit));

        if (!target.IsAlive)
            result.Events.Add(CombatEvent.Death(round, target.Name));
    }

    private static AttackResult ExecuteAttack(CombatActor attacker, CombatActor target)
    {
        int roll = CombatMath.RollDice();
        int total = (int)(attacker.Attack + roll);
        bool hit = total >= target.Ac;
        int damage = 0;

        if (hit)
        {
            damage = CombatMath.CalculateDamage(attacker.Attack);
            target.TakeDamage(damage);
        }

        return new AttackResult(roll, total, damage, hit);
    }
}