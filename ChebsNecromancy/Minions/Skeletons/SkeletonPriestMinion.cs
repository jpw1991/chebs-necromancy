using BepInEx.Configuration;
using ChebsValheimLibrary.Common;

namespace ChebsNecromancy.Minions.Skeletons
{
    internal class SkeletonPriestMinion : SkeletonMinion
    {
        public static MemoryConfigEntry<string, List<string>> ItemsCost;
        public static ConfigEntry<float> HealingPerParticle;

        public static void CreateConfigs(BasePlugin plugin)
        {
            const string serverSyncedHeading = "SkeletonPriest (Server Synced)";
            
            var itemsCost = plugin.ModConfig(serverSyncedHeading, "ItemsCost", "BoneFragments:6,SurtlingCore:1",
                "The items that are consumed when creating a minion. Please use a comma-delimited list of prefab names with a : and integer for amount. Alternative items can be specified with a | eg. Wood|Coal:5 to mean wood and/or coal.",
                null, true);
            ItemsCost = new MemoryConfigEntry<string, List<string>>(itemsCost, s => s?.Split(',').Select(str => str.Trim()).ToList());
            HealingPerParticle = plugin.Config.Bind(serverSyncedHeading, "HealingPerParticle", 10f,
                new ConfigDescription("The amount of healing granted by each particle.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }
    }
}