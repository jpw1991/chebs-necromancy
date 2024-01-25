using ChebsNecromancy.Minions;
using Jotunn.Entities;
using Jotunn.Managers;

namespace ChebsNecromancy.Commands
{
    public class TeleportNeckros : ConsoleCommand
    {
        public override string Name => "chebgonaz_teleportneckros";

        public override string Help => "Teleports all nearby/loaded player-owned Neckros to the player. Admins can ignore ownership with f";

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