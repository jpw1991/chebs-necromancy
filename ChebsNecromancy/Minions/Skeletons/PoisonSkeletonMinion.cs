using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using ChebsValheimLibrary.Common;
using Jotunn;

namespace ChebsNecromancy.Minions.Skeletons
{
    internal class PoisonSkeletonMinion : SkeletonMinion
    {
        public static ConfigEntry<int> LevelRequirementConfig;
        public static ConfigEntry<float> BaseHealth;
        public static MemoryConfigEntry<string, List<string>> ItemsCost;
        
        public new static void CreateConfigs(BasePlugin plugin)
        {
            const string serverSyncedHeading = "PoisonSkeleton (Server Synced)";
            var itemsCost = plugin.ModConfig(serverSyncedHeading, "ItemsCost", "BoneFragments:3,Guck:1",
                "The items that are consumed when creating a minion. Please use a comma-delimited list of prefab names with a : and integer for amount.",
                null, true);
            ItemsCost = new MemoryConfigEntry<string, List<string>>(itemsCost, s => s?.Split(',').ToList());
        }
        
        public override void ScaleStats(float necromancyLevel)
        {
            var character = GetComponent<Character>();
            if (character == null)
            {
                Logger.LogError("ScaleStats: Character component is null!");
                return;
            }
            var health = BaseHealth.Value + necromancyLevel * SkeletonMinion.SkeletonHealthMultiplier.Value;
            character.SetMaxHealth(health);
            character.SetHealth(health);
        }
    }
}
