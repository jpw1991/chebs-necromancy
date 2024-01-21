using ChebsValheimLibrary.PvP;
using Jotunn.Entities;

namespace ChebsNecromancy.Commands
{
    public class PvPRemoveFriend : ConsoleCommand
    {
        public override string Name => "chebgonaz_pvp_friend_remove";

        public override string Help => "Removes a fellow player as your ally.\n" +
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
            friends.RemoveAll(friend => playerNames.Contains(friend));

            PvPManager.UpdatePlayerFriendsDict(friends);
        }

        public override List<string> CommandOptionList()
        {
            return ZNetScene.instance?.GetPrefabNames();
        }
    }
}
