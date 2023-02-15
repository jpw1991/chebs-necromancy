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

            Allowed = plugin.ModConfig("NecroNeckGatherer", "LargeCargoCrateAllowed",
                true, "Disallowing this will cause the ChebGonaz_LargeCargoCrate to not be loaded. Attention: " +
                "it is essential for the correct functioning of the NecroNeck Gatherer minion.", plugin.BoolValue, true);

            ContainerHeight = plugin.ModConfig("NecroNeckGatherer", "LargeCargoCrateHeight",
                2, "Container slots = containerHeight * containerWidth = 5*5 = 25", plugin.IntQuantityValue, true);

            ContainerWidth = plugin.ModConfig("NecroNeckGatherer", "LargeCargoCrateWidth",
                2, "Container slots = containerHeight * containerWidth = 5*5 = 25", plugin.IntQuantityValue, true);
        }
    }
}
