
// console command to kill all player's minions.
// attention: only kills THEIR minions

using ChebsNecromancy.Minions;
using Jotunn.Entities;
using System.Collections.Generic;

namespace ChebsNecromancy.Commands
{
    public class KillAllMinions : ConsoleCommand
    {
        public override string Name => "chebgonaz_killallminions";

        public override string Help => "Kills all of your undead minions (won't kill other players' minions). Admins can ignore the ownership check with f";

        public override void Run(string[] args)
        {
            List<Character> allCharacters = Character.GetAllCharacters();
            // List<Tuple<int, Character>> minionsFound = new();

            bool force = args.Length > 0 && args[0].Equals("f");

            foreach (Character item in allCharacters)
            {
                if (item.IsDead()) continue;
                if (!item.TryGetComponent(out UndeadMinion minion)) continue;
                if (force || minion.BelongsToPlayer(Player.m_localPlayer.GetPlayerName()))
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
