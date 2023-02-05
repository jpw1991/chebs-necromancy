
// console command to kill all player's minions.
// attention: only kills THEIR minions

using System;
using System.Collections.Generic;
using ChebsNecromancy.Minions;
using Jotunn.Entities;
using Jotunn.Managers;

namespace ChebsNecromancy.Commands
{
    public class SetMinionOwnership : ConsoleCommand
    {
        public override string Name => "chebgonaz_setminionownership";

        public override string Help => "Requires admin. Changes all minions' ownership in the specified radius. Usage: [PLAYERNAME] [RADIUS]";

        public override void Run(string[] args)
        {
            if (!SynchronizationManager.Instance.PlayerIsAdmin)
            {
                Console.instance.Print("Only admins can run this command.");
                return;
            }

            if (args.Length < 2)
            {
                Console.instance.Print(Help);
                return;
            }

            if (!Player.GetAllPlayers().Find(player => player.GetPlayerName() == args[0]))
            {
                Console.instance.Print($"Player with name '{args[0]}' not found.");
                return;
            }

            if (!int.TryParse(args[1], out int radius) && radius > 0)
            {
                Console.instance.Print($"Radius of '{args[1]}' is invalid.");
                return;
            }

            float playerNecromancyLevel = Player.m_localPlayer.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill);

            List<Character> characters = new List<Character>();
            Character.GetCharactersInRange(Player.m_localPlayer.transform.position, radius, characters);
            characters.ForEach(character =>
            {
                if (character.TryGetComponent(out UndeadMinion undeadMinion))
                {
                    Console.instance.Print($"Setting '{character.name}'s owner to '{args[0]}' and scaling to {playerNecromancyLevel}.");
                    undeadMinion.SetUndeadMinionMaster(args[0]);
                    undeadMinion.SetCreatedAtLevel(playerNecromancyLevel);

                    // also scale minion health to player's setup
                    if (undeadMinion is SkeletonMinion)
                    {
                        ((SkeletonMinion)undeadMinion).ScaleStats(playerNecromancyLevel);
                    }
                    else if (undeadMinion is DraugrMinion)
                    {
                        ((DraugrMinion)undeadMinion).ScaleStats(playerNecromancyLevel);
                    }
                }
            });
        }

        public override List<string> CommandOptionList()
        {
            return ZNetScene.instance?.GetPrefabNames();
        }
    }
}
