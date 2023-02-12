using BepInEx;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Items
{
    internal class OrbOfBeckoning : Item
    {
        public override string ItemName => "ChebGonaz_OrbOfBeckoning";
        public override string PrefabName => "ChebGonaz_OrbOfBeckoning.prefab";
        public string ProjectilePrefabName => "ChebGonaz_OrbOfBeckoningProjectile.prefab";
        public override string NameLocalization => "$item_chebgonaz_orbofbeckoning";
        public override string DescriptionLocalization => "$item_chebgonaz_orbofbeckoning_desc";
        
        protected override string DefaultRecipe => "Crystal:5,SurtlingCore:5,Tar:25";
        
        public static ConfigEntry<CraftingTable> CraftingStationRequired;
        public static ConfigEntry<int> CraftingStationLevel;
        
        public static ConfigEntry<string> CraftingCost;

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            base.CreateConfigs(plugin);

            Allowed = plugin.Config.Bind("OrbOfBeckoning (Server Synced)", "OrbOfBeckoningAllowed",
                true, new ConfigDescription("Whether crafting an Orb of Beckoning is allowed or not.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingStationRequired = plugin.Config.Bind("OrbOfBeckoning (Server Synced)", "OrbOfBeckoningCraftingStation",
                CraftingTable.Workbench, new ConfigDescription("Crafting station where it's available", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingStationLevel = plugin.Config.Bind("OrbOfBeckoning (Server Synced)", "OrbOfBeckoningCraftingStationLevel",
                1, new ConfigDescription("Crafting station level required to craft.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingCost = plugin.Config.Bind("OrbOfBeckoning (Server Synced)", "OrbOfBeckoningCraftingCosts",
                DefaultRecipe, new ConfigDescription("Materials needed to craft it. None or Blank will use Default settings.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }
        
        public override CustomItem GetCustomItemFromPrefab(GameObject prefab)
        {
            ItemConfig config = new();
            config.Name = NameLocalization;
            config.Description = DescriptionLocalization;

            if (Allowed.Value)
            {
                if (string.IsNullOrEmpty(CraftingCost.Value))
                {
                    CraftingCost.Value = DefaultRecipe;
                }
                // set recipe requirements
                SetRecipeReqs(
                    config,
                    CraftingCost,
                    CraftingStationRequired,
                    CraftingStationLevel
                );
            }
            else
            {
                config.Enabled = false;
            }

            CustomItem customItem = new (prefab, false, config);
            if (customItem == null)
            {
                Logger.LogError($"AddCustomItems: {PrefabName}'s CustomItem is null!");
                return null;
            }
            if (customItem.ItemPrefab == null)
            {
                Logger.LogError($"AddCustomItems: {PrefabName}'s ItemPrefab is null!");
                return null;
            }

            return customItem;
        }
    }
}