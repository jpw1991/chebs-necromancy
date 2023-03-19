using ChebsNecromancy.Minions;
using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Items
{
    internal class SkeletonWoodAxe : Item
    {
        public override string ItemName => "ChebGonaz_SkeletonWoodAxe";
        public override string PrefabName => "ChebGonaz_SkeletonWoodAxe.prefab";
        public override string NameLocalization => "$item_chebgonaz_skeletonaxe_name";
        public override string DescriptionLocalization => "$item_chebgonaz_skeletonaxe_desc";
        
        public override CustomItem GetCustomItemFromPrefab(GameObject prefab)
        {
            ItemConfig config = new ItemConfig();
            config.Name = NameLocalization;
            config.Description = DescriptionLocalization;

            if (prefab.TryGetComponent(out ItemDrop itemDrop))
                itemDrop.m_itemData.m_shared.m_toolTier = SkeletonWoodcutterMinion.ToolTier.Value;

            CustomItem customItem = new CustomItem(prefab, false, config);
            if (customItem == null)
            {
                Logger.LogError($"GetCustomItemFromPrefab: {PrefabName}'s CustomItem is null!");
                return null;
            }
            if (customItem.ItemPrefab == null)
            {
                Logger.LogError($"GetCustomItemFromPrefab: {PrefabName}'s ItemPrefab is null!");
                return null;
            }

            return customItem;
        }
    }
}