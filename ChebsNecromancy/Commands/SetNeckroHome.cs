using System.Collections.Generic;
using ChebsNecromancy.Minions;
using Jotunn.Entities;
using Jotunn.Managers;

namespace ChebsNecromancy.Commands
{
    public class SetNeckroHome : ConsoleCommand
    {
        public override string Name => "chebgonaz_setneckrohome";

        public override string Help => "Sets Neckros' home (on players' minions). Admins can ignore ownership with f";

        public override void Run(string[] args)
        {
            var allCharacters = Character.GetAllCharacters();

            var admin = SynchronizationManager.Instance.PlayerIsAdmin;
            var force = args.Length > 0 && args[0].Equals("f");
            if (force && !admin)
            {
                Console.instance.Print("Only admins can use the force argument with this command.");
                return;
            }

            foreach (var item in allCharacters)
            {
                if (item.IsDead()) continue;
                if (!item.TryGetComponent(out NeckroGathererMinion minion)) continue;
                if (force || minion.BelongsToPlayer(Player.m_localPlayer.GetPlayerName()))
                {
                    minion.Home = Player.m_localPlayer.transform.position;
                }
            }
        }

        public override List<string> CommandOptionList()
        {
            return ZNetScene.instance?.GetPrefabNames();
        }
    }
}