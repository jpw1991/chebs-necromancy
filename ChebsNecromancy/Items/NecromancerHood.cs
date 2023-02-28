using BepInEx;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Items
{
    internal class NecromancerHood : Item
    {
        public override string ItemName => "ChebGonaz_NecromancerHood";
        public override string PrefabName => "ChebGonaz_NecromancerHood.prefab";
        protected override string DefaultRecipe => "WitheredBone:2,TrollHide:5";

        public static ConfigEntry<int> NecromancySkillBonus;

        public static ConfigEntry<CraftingTable> CraftingStationRequired;
        public static ConfigEntry<int> CraftingStationLevel;
        
        public static ConfigEntry<string> CraftingCost;

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            base.CreateConfigs(plugin);

            Allowed = plugin.Config.Bind("NecromancerHood (Server Synced)", "NecromancerHoodAllowed",
                true, new ConfigDescription("Whether crafting a Necromancer's Hood is allowed or not.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingStationRequired = plugin.Config.Bind("NecromancerHood (Server Synced)", "NecromancerHoodCraftingStation",
                CraftingTable.Workbench, new ConfigDescription("Crafting station where Necromancer Hood is available", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingStationLevel = plugin.Config.Bind("NecromancerHood (Server Synced)", "NecromancerHoodCraftingStationLevel",
                1, new ConfigDescription("Crafting station level required to craft Necromancer Hood", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingCost = plugin.Config.Bind("NecromancerHood (Server Synced)", "NecromancerHoodCraftingCosts",
               DefaultRecipe, new ConfigDescription("Materials needed to craft Necromancer Hood. None or Blank will use Default settings.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            NecromancySkillBonus = plugin.Config.Bind("NecromancerHood (Server Synced)", "NecromancerHoodSkillBonus",
                10, new ConfigDescription("How much wearing the item should raise the Necromancy level (set to 0 to have no set effect at all).", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        public override void UpdateRecipe()
        {
            UpdateRecipe(CraftingStationRequired, CraftingCost, CraftingStationLevel);
        }

        public override CustomItem GetCustomItemFromPrefab(GameObject prefab)
        {
            ItemConfig config = new ItemConfig();
            config.Name = "$item_chebgonaz_necromancerhood";
            config.Description = "$item_chebgonaz_necromancerhood_desc";

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

            CustomItem customItem = new CustomItem(prefab, false, config);
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
            // make sure the set effect is applied or removed according
            // to config values
            customItem.ItemDrop.m_itemData.m_shared.m_setStatusEffect =
                NecromancySkillBonus.Value > 0 ?
                BasePlugin.SetEffectNecromancyArmor2 : null;
            customItem.ItemDrop.m_itemData.m_shared.m_equipStatusEffect =
                NecromancySkillBonus.Value > 0 ?
                BasePlugin.SetEffectNecromancyArmor2 : null;

            return customItem;
        }
    }
}
