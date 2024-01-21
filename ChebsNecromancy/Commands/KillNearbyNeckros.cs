﻿
// console command to kill all player's neckros.
// attention: only kills THEIR minions

using ChebsNecromancy.Minions;
using Jotunn.Entities;
using Jotunn.Managers;

namespace ChebsNecromancy.Commands
{
    public class KillAllNeckros : ConsoleCommand
    {
        public override string Name => "chebgonaz_killallneckros";

        public override string Help => "Kills Neckros (won't kill other players' minions). Admins can ignore ownership with f";

        public override void Run(string[] args)
        {
            List<Character> allCharacters = Character.GetAllCharacters();
            
            bool admin = SynchronizationManager.Instance.PlayerIsAdmin;
            bool force = args.Length > 0 && args[0].Equals("f");
            if (force && !admin)
            {
                Console.instance.Print("Only admins can use the force argument with this command.");
                return;
            }
            
            foreach (Character item in allCharacters)
            {
                if (item.IsDead()) continue;
                if (!item.TryGetComponent(out NeckroGathererMinion minion)) continue;
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
