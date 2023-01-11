using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;

namespace FriendlySkeletonWand
{
    internal class BlackIronHelmet : Item
    {
        public override string ItemName { get { return "ChebGonaz_HelmetBlackIron"; } }
        public override string PrefabName { get { return "ChebGonaz_HelmetBlackIron.prefab"; } }
        protected override string DefaultRecipe { get { return "BlackMetal:5"; } }

        public CustomItem GetCustomItemFromPrefab(GameObject prefab)
        {
            ItemConfig config = new ItemConfig();
            config.Name = "ChebGonaz_HelmetBlackIron";
            config.Description = "ChebGonaz_HelmetBlackIron";

            CustomItem customItem = new CustomItem(prefab, false, config);
            if (customItem == null)
            {
                Jotunn.Logger.LogError($"GetCustomItemFromPrefab: {PrefabName}'s CustomItem is null!");
                return null;
            }
            if (customItem.ItemPrefab == null)
            {
                Jotunn.Logger.LogError($"GetCustomItemFromPrefab: {PrefabName}'s ItemPrefab is null!");
                return null;
            }

            return customItem;
        }
    }
}
