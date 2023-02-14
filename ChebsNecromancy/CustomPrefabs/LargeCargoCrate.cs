using BepInEx;
using BepInEx.Configuration;

namespace ChebsNecromancy.CustomPrefabs
{
    internal class LargeCargoCrate
    {
        // large cargo crate is dropped by NecroNeckGathererMinions
        public const string PrefabName = "ChebGonaz_LargeCargoCrate.prefab";

        public static ConfigEntry<bool> Allowed;
        public static ConfigEntry<int> ContainerHeight, ContainerWidth;

        public static void CreateConfigs(BasePlugin plugin)
        {

            Allowed = plugin.ModConfig("NecroNeckGatherer (Server Synced)", "LargeCargoCrateAllowed",
                true, new ConfigDescription("Disallowing this will cause the ChebGonaz_LargeCargoCrate to not be loaded. Attention: it is essential for the correct functioning of the NecroNeck Gatherer minion.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            ContainerHeight = plugin.ModConfig("NecroNeckGatherer (Server Synced)", "LargeCargoCrateHeight",
                2, new ConfigDescription("Container slots = containerHeight * containerWidth = 5*5 = 25", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            ContainerWidth = plugin.ModConfig("NecroNeckGatherer (Server Synced)", "LargeCargoCrateWidth",
                2, new ConfigDescription("Container slots = containerHeight * containerWidth = 5*5 = 25", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }
    }
}
