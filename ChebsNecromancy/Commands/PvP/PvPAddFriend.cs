using ChebsValheimLibrary.PvP;
using Jotunn.Entities;

namespace ChebsNecromancy.Commands
{
    public class PvPAddFriend : ConsoleCommand
    {
        public override string Name => "chebgonaz_pvp_friend_add";

        public override string Help => "Registers a fellow player as your ally. If both you and your ally register " +
                                       "each other in this way, your minions won't fight each other.\n" +
                                       $"Usage: {Name} [NAME1] [NAME2] ...\n" +
                                       $"eg. {Name} Bob Billy Jane";

        public override void Run(string[] args)
        {
            if (args.Length < 1)
            {
                Console.instance.Print(Help);
                return;
            }

            var playerNames = args.Select(s => s.Trim()).ToList();
            var friends = PvPManager.GetPlayerFriends();
            foreach (var playerName in playerNames)
            {
                if (!friends.Contains(playerName))
                {
                    friends.Add(playerName);
                }
            } 
            PvPManager.UpdatePlayerFriendsDict(friends);
        }

        public override List<string> CommandOptionList()
        {
            return ZNetScene.instance?.GetPrefabNames();
        }
    }
}
