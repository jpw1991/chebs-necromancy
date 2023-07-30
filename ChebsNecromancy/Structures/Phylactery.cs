using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.Structures;
using UnityEngine;

namespace ChebsNecromancy.Structures
{
    internal class Phylactery : Structure
    {
        public static List<Phylactery> Phylacteries = new ();

        public static ConfigEntry<string> FuelPrefab;

        private Container _container;
        private Inventory _inventory;

        public new static ChebsRecipe ChebsRecipeConfig = new()
        {
            DefaultRecipe = "Stone:100,Coal:100",
            IconName = "chebgonaz_phylactery_icon.png",
            PieceTable = "_HammerPieceTable",
            PieceCategory = "Misc",
            PieceName = "$chebgonaz_phylactery_name",
            PieceDescription = "$chebgonaz_phylactery_desc",
            PrefabName = "ChebGonaz_Phylactery.prefab",
            ObjectName = MethodBase.GetCurrentMethod().DeclaringType.Name
        };

        public new static void UpdateRecipe()
        {
            ChebsRecipeConfig.UpdateRecipe(ChebsRecipeConfig.CraftingCost);
        }

        public static void CreateConfigs(BasePlugin plugin)
        {
            ChebsRecipeConfig.Allowed = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "PhylacteryAllowed", true,
                "Whether making a Phylactery  is allowed or not.", plugin.BoolValue, true);

            ChebsRecipeConfig.CraftingCost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "PhylacteryBuildCosts",
                ChebsRecipeConfig.DefaultRecipe,
                "Materials needed to build a Phylactery . None or Blank will use Default settings. Format: " +
                ChebsRecipeConfig.RecipeValue,
                null, true);
            
            FuelPrefab = plugin.Config.Bind(ChebsRecipeConfig.ObjectName, "Fuel",
                "DragonEgg", new ConfigDescription("The prefab name that is consumed as fuel.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        public bool ConsumeFuel()
        {
            var fuelPrefab = ZNetScene.instance.GetPrefab(FuelPrefab.Value);
            if (fuelPrefab == null)
            {
                Jotunn.Logger.LogError("Phylactery.ConsumeFuel: fuelPrefab is null");
                return false;
            }
            
            if (!fuelPrefab.TryGetComponent(out ItemDrop itemDrop))
            {
                Jotunn.Logger.LogError("Phylactery.ConsumeFuel: fuelPrefab has no ItemDrop");
                return false;
            }

            // why in the nine hells won't this work?
            //return _inventory.RemoveOneItem(fuelPrefab.GetComponent<ItemDrop>().m_itemData);

            if (_inventory.HaveItem(itemDrop.m_itemData.m_shared.m_name))
            {
                _inventory.RemoveItem(itemDrop.m_itemData.m_shared.m_name, 1);
                return true;
            }
            return false;
        }

        private void Awake()
        {
            StartCoroutine(Wait());
        }

        private IEnumerator Wait()
        {
            var piece = GetComponent<Piece>();
            yield return new WaitWhile(() => !piece.IsPlacedByPlayer());
            
            if (!Phylacteries.Contains(this))
                Phylacteries.Add(this);
            
            _container = gameObject.AddComponent<Container>();
            _container.m_name = "$chebgonaz_refuelerpylon_name";

            _inventory = _container.GetInventory();
            _inventory.m_name = Localization.instance.Localize(_container.m_name);
        }
    }
}