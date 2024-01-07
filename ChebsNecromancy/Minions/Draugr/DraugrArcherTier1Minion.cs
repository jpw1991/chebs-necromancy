using System.Collections.Generic;
using System.Linq;
using ChebsValheimLibrary.Common;

namespace ChebsNecromancy.Minions.Draugr
{
    internal class DraugrArcherTier1Minion : DraugrMinion
    {
        public static MemoryConfigEntry<string, List<string>> ItemsCost;

        public static void CreateConfigs(BasePlugin plugin)
        {
            const string serverSyncedHeading = "DraugrArcherTier1 (Server Synced)";
            
            var itemsCost = plugin.ModConfig(serverSyncedHeading, "ItemsCost", 
                "BoneFragments:6,RawMeat|DeerMeat|WolfMeat|LoxMeat|SerpentMeat|HareMeat|BugMeat|ChickenMeat:2,ArrowWood:10",
                "The items that are consumed when creating a minion. Please use a comma-delimited list of prefab names with a : and integer for amount. Alternative items can be specified with a | eg. Wood|Coal:5 to mean wood and/or coal.",
                null, true);
            ItemsCost = new MemoryConfigEntry<string, List<string>>(itemsCost, s => s?.Split(',').Select(str => str.Trim()).ToList());
        }
    }
}