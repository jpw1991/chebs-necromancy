using BepInEx.Configuration;
using Jotunn.Entities;
using System.Reflection;
using ChebsNecromancy.Common;
using UnityEngine;

namespace ChebsNecromancy.Items.PlayerItems
{
    internal class NecromancerHood : Item
    {
        public static ConfigEntry<int> NecromancySkillBonus;

        public override void CreateConfigs(BasePlugin plugin)
        {
            ChebsRecipeConfig.DefaultRecipe = "WitheredBone:2,TrollHide:5";
            ChebsRecipeConfig.RecipeName = "$item_chebgonaz_necromancerhood";
            ChebsRecipeConfig.ItemName = "ChebGonaz_NecromancerHood";
            ChebsRecipeConfig.RecipeDescription = "$item_chebgonaz_necromancerhood_desc";
            ChebsRecipeConfig.PrefabName = "ChebGonaz_NecromancerHood.prefab";
            ChebsRecipeConfig.ObjectName = MethodBase.GetCurrentMethod().DeclaringType.Name;

            base.CreateConfigs(plugin);

            ChebsRecipeConfig.Allowed = plugin.ModConfig(ChebsRecipeConfig.ObjectName, ChebsRecipeConfig.ObjectName + "Allowed",
                true, "Whether crafting a Necromancer's Hood is allowed or not.", plugin.BoolValue, true);

            ChebsRecipeConfig.CraftingStationRequired = plugin.ModConfig(ChebsRecipeConfig.ObjectName, ChebsRecipeConfig.ObjectName + "CraftingStation",
                ChebsRecipe.EcraftingTable.Workbench, "Crafting station where Necromancer Hood is available", null, true);

            ChebsRecipeConfig.CraftingStationLevel = plugin.ModConfig(ChebsRecipeConfig.ObjectName, ChebsRecipeConfig.ObjectName + "CraftingStationLevel",
                1, "Crafting station level required to craft Necromancer Hood", plugin.IntQuantityValue, true);

            ChebsRecipeConfig.CraftingCost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, ChebsRecipeConfig.ObjectName + "CraftingCosts",
               ChebsRecipeConfig.DefaultRecipe, "Materials needed to craft Necromancer Hood. None or Blank will use Default settings.", null, true);

            NecromancySkillBonus = plugin.ModConfig(ChebsRecipeConfig.ObjectName, ChebsRecipeConfig.ObjectName + "SkillBonus",
                10, "How much wearing the item should raise the Necromancy level (set to 0 to have no set effect at all).", plugin.IntQuantityValue, true);
        }
        public CustomItem GetCustomItemFromPrefab(GameObject prefab)
        {
            CustomItem customItem = ChebsRecipeConfig.GetCustomItemFromPrefab<CustomItem>(prefab);

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
