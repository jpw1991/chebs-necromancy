
// console command to kill all player's neckros.
// attention: only kills THEIR minions

using System;
using System.Collections.Generic;
using ChebsNecromancy.Minions;
using Jotunn.Entities;

namespace ChebsNecromancy.Commands
{
    public class KillAllNeckros : ConsoleCommand
    {
        public override string Name => "chebgonaz_killallneckros";

        public override string Help => "Kills Neckros (won't kill other players' minions)";

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

                NeckroGathererMinion minion = item.GetComponent<NeckroGathererMinion>();
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
