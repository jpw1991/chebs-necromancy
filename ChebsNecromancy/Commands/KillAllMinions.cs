
// console command to kill all player's minions.
// attention: only kills THEIR minions

using System;
using System.Collections.Generic;
using ChebsNecromancy.Minions;
using Jotunn.Entities;

namespace ChebsNecromancy.Commands
{
    public class KillAllMinions : ConsoleCommand
    {
        public override string Name => "chebgonaz_killallminions";

        public override string Help => "Kills all of your undead minions (won't kill other players' minions)";

        public override void Run(string[] args)
        {
            List<Character> allCharacters = Character.GetAllCharacters();
            List<Tuple<int, Character>> minionsFound = new List<Tuple<int, Character>>();

            foreach (Character item in allCharacters)
            {
                if (item.IsDead())
                {
                    continue;
                }

                UndeadMinion minion = item.GetComponent<UndeadMinion>();
                if (minion != null 
                    && minion.BelongsToPlayer(Player.m_localPlayer.GetPlayerName()))
                {
                    item.SetHealth(0);
                }
            }
        }

        public override List<string> CommandOptionList()
        {
            return ZNetScene.instance?.GetPrefabNames();
        }
    }
}
