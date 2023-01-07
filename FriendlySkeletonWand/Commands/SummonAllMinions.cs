
// console command to summon player's minions.
// attention: only summons THEIR minions

using System;
using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace FriendlySkeletonWand.Commands
{
    public class SummonAllMinions : ConsoleCommand
    {
        public override string Name => "chebgonaz_summonallminions";

        public override string Help => "Summons all of your undead minions to your location (won't summon other players' minions)";

        public override void Run(string[] args)
        {
            List<Character> allCharacters = Character.GetAllCharacters();
            List<Tuple<int, Character>> minionsFound = new List<Tuple<int, Character>>();

            foreach (Character item in allCharacters)
            {
                if (item.IsDead() || !item.IsOwner())
                {
                    continue;
                }

                UndeadMinion minion = item.GetComponent<UndeadMinion>();
                if (minion != null)
                {
                    minion.transform.position = Player.m_localPlayer.transform.position;
                }
            }
        }

        public override List<string> CommandOptionList()
        {
            return ZNetScene.instance?.GetPrefabNames();
        }
    }
}
