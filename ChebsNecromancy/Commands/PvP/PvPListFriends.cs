using System.Collections.Generic;
using System.Linq;
using ChebsNecromancy.Minions;
using ChebsNecromancy.Minions.Draugr;
using ChebsNecromancy.Minions.Skeletons;
using ChebsValheimLibrary.PvP;
using Jotunn.Entities;
using Jotunn.Managers;

namespace ChebsNecromancy.Commands
{
    public class PvPListFriends : ConsoleCommand
    {
        public override string Name => "chebgonaz_pvp_friend_list";

        public override string Help => "Lists your current PvP friends.";

        public override void Run(string[] args)
        {
            var friends = PvPManager.GetPlayerFriends();
            Console.instance.Print(string.Join(" ", friends));
        }

        public override List<string> CommandOptionList()
        {
            return ZNetScene.instance?.GetPrefabNames();
        }
    }
}
