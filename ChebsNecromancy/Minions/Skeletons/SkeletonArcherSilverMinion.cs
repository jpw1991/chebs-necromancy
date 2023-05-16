using System.Collections.Generic;
using System.Linq;
using ChebsValheimLibrary.Common;

namespace ChebsNecromancy.Minions.Skeletons
{
    internal class SkeletonArcherSilverMinion : SkeletonMinion
    {
        public static MemoryConfigEntry<string, List<string>> ItemsCost;

        public new static void CreateConfigs(BasePlugin plugin)
        {
            const string serverSyncedHeading = "SkeletonArcherSilver (Server Synced)";
            
            var itemsCost = plugin.ModConfig(serverSyncedHeading, "ItemsCost", "BoneFragments:6,ArrowSilver:10",
                "The items that are consumed when creating a minion. Please use a comma-delimited list of prefab names with a : and integer for amount. Alternative items can be specified with a | eg. Wood|Coal:5 to mean wood and/or coal.",
                null, true);
            ItemsCost = new MemoryConfigEntry<string, List<string>>(itemsCost, s => s?.Split(',').ToList());
        }
    }
}