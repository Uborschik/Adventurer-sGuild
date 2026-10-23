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

        var enemies = CombatActorFactory.FromEncounter(encounter);

        if (enemies.Count == 0)
        {
            result.PartyWon = true;
            result.Events.Add(CombatEvent.Victory(0, "Пустая встреча"));
            return result;
        }

        result.Events.Add(CombatEvent.Start($"{enemies.Count} враг(ов)", 0, 0));

        var turnOrder = new List<CombatActor>(party.Count + enemies.Count);
        turnOrder.AddRange(party);
        turnOrder.AddRange(enemies);
        turnOrder.Sort((a, b) => b.Initiative.CompareTo(a.Initiative));

        var round = 1;

        while (round <= rules.MaxRounds && HasAlive(enemies) && HasAlive(party))
        {
            result.Events.Add(CombatEvent.RoundStart(round));

            for (int i = 0; i < turnOrder.Count; i++)
            {
                var actor = turnOrder[i];
                if (!actor.IsAlive) continue;
                if (!HasAlive(enemies) || !HasAlive(party)) break;

                if (actor.IsEnemy)
                {
                    EnemyTurn(actor, party, result, round);
                }
                else
                {
                    HeroTurn(actor, enemies, result, round);
                }
            }
            round++;
        }

        if (!HasAlive(enemies))
        {
            result.PartyWon = true;
            result.Events.Add(CombatEvent.Victory(round, "Все враги"));
        }
        else if (!HasAlive(party))
        {
            result.PartyWon = false;
            result.Events.Add(CombatEvent.Defeat(round));
        }
        else
        {
            result.PartyWon = false;
            result.Events.Add(CombatEvent.Timeout(round));
        }

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

    private static bool HasAlive(List<CombatActor> actors)
    {
        for (int i = 0; i < actors.Count; i++)
            if (actors[i].IsAlive) return true;
        return false;
    }

    private static void HeroTurn(CombatActor hero, List<CombatActor> enemies, CombatResult result, int round)
    {
        var aliveEnemies = new List<CombatActor>();
        foreach (var e in enemies) if (e.IsAlive) aliveEnemies.Add(e);

        if (aliveEnemies.Count == 0) return;

        var target = aliveEnemies[Random.Shared.Next(aliveEnemies.Count)];
        var attackRes = ExecuteAttack(hero, target);

        result.Events.Add(CombatEvent.Attack(
            round, hero.Name, target.Name, attackRes.Roll, attackRes.Total,
            target.Ac, attackRes.Damage, target.CurrentHp, target.MaxHp, attackRes.Hit));

        if (!target.IsAlive)
            result.Events.Add(CombatEvent.Death(round, target.Name));
    }

    private static void EnemyTurn(CombatActor enemy, List<CombatActor> party, CombatResult result, int round)
    {
        var aliveParty = new List<CombatActor>();
        foreach (var p in party) if (p.IsAlive) aliveParty.Add(p);

        if (aliveParty.Count == 0) return;

        var target = aliveParty[Random.Shared.Next(aliveParty.Count)];
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