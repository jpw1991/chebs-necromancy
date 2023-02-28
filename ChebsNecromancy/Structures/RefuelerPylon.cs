using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using ChebsNecromancy.Common;
using UnityEngine;

namespace ChebsNecromancy.Structures
{
    internal class RefuelerPylon : Structure
    {
        public static ConfigEntry<float> SightRadius;
        public static ConfigEntry<float> RefuelerUpdateInterval;
        public static ConfigEntry<int> RefuelerContainerWidth, RefuelerContainerHeight;
        public static ConfigEntry<bool> ManageFireplaces, ManageSmelters, ManageCookingStations;

        private readonly int pieceMask = LayerMask.GetMask("piece");
        private readonly int pieceMaskNonSolid = LayerMask.GetMask("piece_nonsolid");

        private Container _container;
        private Inventory _inventory;

        public new static ChebsRecipe ChebsRecipeConfig = new()
        {
            DefaultRecipe = "Stone:15,Coal:15,BoneFragments:15,SurtlingCore:1",
            IconName = "chebgonaz_refuelerpylon_icon.png",
            PieceTable = "_HammerPieceTable",
            PieceCategory = "Misc",
            PieceName = "$chebgonaz_refuelerpylon_name",
            PieceDescription = "$chebgonaz_refuelerpylon_desc",
            PrefabName = "ChebGonaz_RefuelerPylon.prefab",
            ObjectName = MethodBase.GetCurrentMethod().DeclaringType.Name
        };

        public new static void UpdateRecipe()
        {
            ChebsRecipeConfig.UpdateRecipe(ChebsRecipeConfig.CraftingCost);
        }
        
        public static void CreateConfigs(BasePlugin plugin)
        {
            ChebsRecipeConfig.Allowed = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "RefuelerPylonAllowed", true,
                "Whether making a Refueler Pylon is allowed or not.", plugin.BoolValue, true);

            ChebsRecipeConfig.CraftingCost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "RefuelerPylonBuildCosts",
                ChebsRecipeConfig.DefaultRecipe,
                "Materials needed to build a Refueler Pylon. None or Blank will use Default settings. Format: " +
                ChebsRecipeConfig.RecipeValue,
                null, true);

            SightRadius = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "RefuelerPylonSightRadius", 30f,
                "How far a Refueler Pylon can reach containers.", plugin.FloatQuantityValue, true);

            RefuelerUpdateInterval = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "RefuelerPylonUpdateInterval", 5f,
                "How long a Refueler Pylon waits between checking containers (lower values may negatively impact performance).",
                plugin.FloatQuantityValue, true);

            RefuelerContainerWidth = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "RefuelerPylonContainerWidth", 4,
                "Inventory size = width * height = 4 * 4 = 16.", new AcceptableValueRange<int>(2, 10), true);

            RefuelerContainerHeight = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "RefuelerPylonContainerHeight", 4,
                "Inventory size = width * height = 4 * 4 = 16.", new AcceptableValueRange<int>(4, 20), true);

            ManageFireplaces = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "ManageFireplaces", true,
                "Whether making a Refueler Pylon will manage fireplaces.", plugin.BoolValue, true);

            ManageSmelters = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "ManageSmelters", true,
                "Whether making a Refueler Pylon will manage smelters.", plugin.BoolValue, true);

            ManageCookingStations = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "ManageCookingStations", true,
                "Whether making a Refueler Pylon will manage cooking stations.", plugin.BoolValue, true);
        }

#pragma warning disable IDE0051 // Remove unused private members
        private void Awake()
#pragma warning restore IDE0051 // Remove unused private members
        {
            _container = GetComponent<Container>();
            _inventory = _container.GetInventory();

            _container.m_width = RefuelerContainerWidth.Value;
            _container.m_height = RefuelerContainerHeight.Value;

            StartCoroutine(LookForPieces());
        }

        IEnumerator LookForPieces()
        {
            yield return new WaitWhile(() => ZInput.instance == null);

            // prevent coroutine from doing its thing while the pylon isn't
            // yet constructed
            var piece = GetComponent<Piece>();
            yield return new WaitWhile(() => !piece.IsPlacedByPlayer());

            while (true)
            {
                yield return new WaitForSeconds(RefuelerUpdateInterval.Value);

                Tuple<List<Smelter>, List<Fireplace>, List<CookingStation>> tuple = GetNearbySmeltersAndFireplaces();

                List<Smelter> smelters = tuple.Item1;
                if (smelters != null) smelters.ForEach(ManageSmelter);

                List<Fireplace> fireplaces = tuple.Item2;
                if (fireplaces != null) fireplaces.ForEach(ManageFireplace);

                var cookingStations = tuple.Item3;
                if (cookingStations != null) cookingStations.ForEach(ManageCookingStation);
            }
        }

        private Tuple<List<Smelter>, List<Fireplace>, List<CookingStation>> GetNearbySmeltersAndFireplaces()
        {
            // find and return smelters and fireplaces in range
            var nearbyColliders = Physics.OverlapSphere(transform.position + Vector3.up, SightRadius.Value, pieceMask);
            if (nearbyColliders.Length < 1) return null;

            List<Smelter> smelters = new();
            List<Fireplace> fireplaces = new();
            List<CookingStation> cookingStations = new();
            foreach (var nearbyCollider in nearbyColliders)
            {
                if (ManageSmelters.Value)
                {
                    Smelter smelter = nearbyCollider.GetComponentInParent<Smelter>();
                    if (smelter != null) smelters.Add(smelter);
                }

                if (ManageFireplaces.Value)
                {
                    Fireplace fireplace = nearbyCollider.GetComponentInParent<Fireplace>();
                    if (fireplace != null) fireplaces.Add(fireplace);
                }
            }

            if (ManageCookingStations.Value)
            {
                var nearbyPieceNonSolidColliders = Physics.OverlapSphere(transform.position + Vector3.up,
                    SightRadius.Value, pieceMaskNonSolid);
                foreach (var nearbyPieceNonSolidCollider in nearbyPieceNonSolidColliders)
                {
                    CookingStation cookingStation = nearbyPieceNonSolidCollider.GetComponentInParent<CookingStation>();
                    if (cookingStation != null) cookingStations.Add(cookingStation);
                }
            }

            return new Tuple<List<Smelter>, List<Fireplace>, List<CookingStation>>(smelters, fireplaces,
                cookingStations);
        }

        private void ManageSmelter(Smelter smelter)
        {
            // fuel types
            // smelters:
            //     "$item_coal"
            // kilns (also technically smelters):
            //     "$item_wood"
            //     "$item_roundlog" -> core wood
            //     "$item_finewood"

            if (smelter == null) return;

            void LoadSmelterWithFuel(string fuel)
            {
                while (_inventory.CountItems(fuel) > 0)
                {
                    float currentFuel = smelter.GetFuel();
                    if (currentFuel < smelter.m_maxFuel)
                    {
                        smelter.SetFuel(currentFuel + 1);
                        _inventory.RemoveItem(fuel, 1);
                    }
                    else
                    {
                        // smelter full
                        break;
                    }
                }
            }

            // load smelter with fuel -> coal (smelter)
            if (smelter.m_fuelItem != null)
            {
                // kilns require no fuel, so we gotta null check
                LoadSmelterWithFuel(smelter.m_fuelItem.m_itemData.m_shared.m_name);
            }

            // load smelter with any item conversions
            // eg.
            // copper ore --> copper
            // wood --> coal
            ItemDrop.ItemData itemData = smelter.FindCookableItem(_inventory);
            if (itemData != null)
            {
                // adapted from Smelter.OnAddOre

                if (!smelter.IsItemAllowed(itemData.m_dropPrefab.name))
                {
                    return;
                }

                if (smelter.GetQueueSize() >= smelter.m_maxOre)
                {
                    return;
                }

                _inventory.RemoveItem(itemData, 1);
                smelter.m_nview.InvokeRPC("AddOre", itemData.m_dropPrefab.name);
                smelter.m_addedOreTime = Time.time;
                if (smelter.m_addOreAnimationDuration > 0f)
                {
                    smelter.SetAnimation(true);
                }
            }
        }

        private void ManageFireplace(Fireplace fireplace)
        {
            float currentFuel = fireplace.m_nview.GetZDO().GetFloat("fuel");
            // fuel is always an incomplete number like 5.98/6.00 because the moment you add the fuel
            // it begins decreasing. So minus 1 from the max so we only add fuel if it is something like
            // 4.98/6.00
            if (currentFuel >= fireplace.m_maxFuel - 1) return;

            if (_inventory.HaveItem(fireplace.m_fuelItem.m_itemData.m_shared.m_name))
            {
                fireplace.m_nview.InvokeRPC("AddFuel");
                _inventory.RemoveItem(fireplace.m_fuelItem.m_itemData.m_shared.m_name, 1);
            }
        }

        private void ManageCookingStation(CookingStation cookingStation)
        {
            //remove cooked items
            if (cookingStation.HaveDoneItem())
            {
                cookingStation.m_nview.InvokeRPC("RemoveDoneItem", transform.position);
            }

            // add cookable items
            var freeSlot = cookingStation.GetFreeSlot();
            if (freeSlot == -1) return;

            var cookableItem = cookingStation.FindCookableItem(_inventory);
            if (cookableItem != null)
            {
                var cookableItemName = cookableItem.m_dropPrefab.name;
                cookingStation.m_nview.InvokeRPC("AddItem", cookableItemName);
                _inventory.RemoveOneItem(cookableItem);
            }
        }
    }
}