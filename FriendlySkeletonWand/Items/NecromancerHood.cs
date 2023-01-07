using Jotunn.Configs;
using Jotunn.Entities;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using Jotunn.Managers;

namespace FriendlySkeletonWand
{
    internal class NecromancerHood : Item
    {
        public override string ItemName { get { return "ChebGonaz_NecromancerHood"; } }
        public override string PrefabName { get { return "ChebGonaz_NecromancerHood.prefab"; } }

        public static ConfigEntry<int> necromancySkillBonus;

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            base.CreateConfigs(plugin);

            allowed = plugin.Config.Bind("Server config", "NecromancerHoodAllowed",
                true, new ConfigDescription("Whether crafting a Necromancer's Hood is allowed or not.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            necromancySkillBonus = plugin.Config.Bind("Server config", "NecromancerHoodSkillBonus",
                10, new ConfigDescription("How much wearing the item should raise the Necromancy level (set to 0 to have no set effect at all).", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        public override CustomItem GetCustomItem(Sprite icon=null)
        {
            Jotunn.Logger.LogError("I shouldn't be called");
            return null;
        }

        public CustomItem GetCustomItemFromPrefab(GameObject prefab)
        {
            ItemConfig config = new ItemConfig();
            config.Name = "$item_chebgonaz_necromancerhood";
            config.Description = "$item_chebgonaz_necromancerhood_desc";
            if (allowed.Value)
            {
                config.CraftingStation = "piece_workbench";
                config.AddRequirement(new RequirementConfig("WitheredBone", 2));
                config.AddRequirement(new RequirementConfig("TrollHide", 5));
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
            // make sure the set effect is applied or removed according
            // to config values
            customItem.ItemDrop.m_itemData.m_shared.m_setStatusEffect =
                necromancySkillBonus.Value > 0 ?
                BasePlugin.setEffectNecromancyArmor2 : null;
            customItem.ItemDrop.m_itemData.m_shared.m_equipStatusEffect =
                necromancySkillBonus.Value > 0 ?
                BasePlugin.setEffectNecromancyArmor2 : null;

            return customItem;
        }
    }
}
