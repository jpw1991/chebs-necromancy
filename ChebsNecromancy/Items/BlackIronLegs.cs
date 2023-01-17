﻿using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;

namespace ChebsNecromancy
{
    internal class BlackIronLegs : Item
    {
        public override string ItemName { get { return "ChebGonaz_ArmorBlackIronLegs"; } }
        public override string PrefabName { get { return "ChebGonaz_ArmorBlackIronLegs.prefab"; } }
        protected override string DefaultRecipe { get { return "BlackMetal:5"; } }

        public CustomItem GetCustomItemFromPrefab(GameObject prefab)
        {
            ItemConfig config = new ItemConfig();
            config.Name = "ChebGonaz_ArmorBlackIronLegs";
            config.Description = "ChebGonaz_ArmorBlackIronLegs";

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