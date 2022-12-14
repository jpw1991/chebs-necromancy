using Jotunn.Configs;
using Jotunn.Entities;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace FriendlySkeletonWand
{
    internal class SkeletonClub : Item
    {
        public const string prefabName = "ChebGonaz_SkeletonClub.prefab";

        public SkeletonClub()
        {
            ItemName = "ChebGonaz_SkeletonClub";
        }

        public override CustomItem GetCustomItem(Sprite icon=null)
        {
            Jotunn.Logger.LogError("I shouldn't be called");
            return null;
        }

        public CustomItem GetCustomItemFromPrefab(GameObject prefab)
        {
            ItemConfig config = new ItemConfig();
            config.Name = "$item_chebgonaz_skeletonclub";
            config.Description = "$item_chebgonaz_skeletonclub_desc";

            CustomItem customItem = new CustomItem(prefab, false, config);
            if (customItem == null)
            {
                Jotunn.Logger.LogError($"GetCustomItemFromPrefab: {prefabName}'s CustomItem is null!");
                return null;
            }
            if (customItem.ItemPrefab == null)
            {
                Jotunn.Logger.LogError($"GetCustomItemFromPrefab: {prefabName}'s ItemPrefab is null!");
                return null;
            }

            return customItem;
        }
    }
}
