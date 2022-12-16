using Jotunn.Configs;
using Jotunn.Entities;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using Jotunn.Managers;

namespace FriendlySkeletonWand
{
    internal class SpectralShroud : Item
    {
        public override string ItemName { get { return "ChebGonaz_SpectralShroud"; } }
        public override string PrefabName { get { return "ChebGonaz_SpectralShroud.prefab"; } }

        public static ConfigEntry<bool> spawnWraith;

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            base.CreateConfigs(plugin);

            allowed = plugin.Config.Bind("Client config", "SpectralShroudAllowed",
                true, new ConfigDescription("Whether crafting a Spectral Shroud is allowed or not."));

            spawnWraith = plugin.Config.Bind("Client config", "SpectralShroudSpawnWraith",
                true, new ConfigDescription("Whether wraiths spawn or not."));
        }

        public override CustomItem GetCustomItem(Sprite icon=null)
        {
            Jotunn.Logger.LogError("I shouldn't be called");
            return null;
        }

        public CustomItem GetCustomItemFromPrefab(GameObject prefab)
        {
            ItemConfig config = new ItemConfig();
            config.Name = "$item_friendlyskeletonwand_spectralshroud";
            config.Description = "$item_friendlyskeletonwand_spectralshroud_desc";
            if (allowed.Value)
            {
                config.CraftingStation = "piece_workbench";
                config.AddRequirement(new RequirementConfig("Chain", 5));
                config.AddRequirement(new RequirementConfig("TrollHide", 10));
            }

            CustomItem customItem = new CustomItem(prefab, false, config);
            if (customItem == null)
            {
                Jotunn.Logger.LogError($"AddCustomItems: {PrefabName}'s CustomItem is null!");
                return null;
            }
            if (customItem.ItemPrefab == null)
            {
                Jotunn.Logger.LogError($"AddCustomItems: {PrefabName}'s ItemPrefab is null!");
                return null;
            }

            return customItem;
        }
    }
}
