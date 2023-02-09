using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using ChebsNecromancy.Common;
using UnityEngine;

namespace ChebsNecromancy.Structures
{
    internal class RefuelerPylon : MonoBehaviour
    {
        public static ConfigEntry<float> SightRadius;
        public static ConfigEntry<float> RefuelerUpdateInterval;
        public static ConfigEntry<int> RefuelerContainerWidth, RefuelerContainerHeight;
        protected static int PieceMask;
        protected static Container Container;

        public static ChebsRecipe ChebsRecipeConfig = new()
        {
            DefaultRecipe = "Stone:15,Coal:15,BoneFragments:15,SurtlingCore:1",
            IconName = "chebgonaz_refuelerpylon_icon.png",
            PieceTable = "_HammerPieceTable",
            PieceCategory = "Misc",
            PieceName = "$chebgonaz_refuelerpylon_name",
            PieceDescription = "$chebgonaz_refuelerpylon_desc",
            PrefabName = "ChebGonaz_RefuelerPylon.prefab",
        };

        public static void CreateConfigs(BasePlugin plugin)
        {
            ChebsRecipeConfig.Allowed = plugin.ModConfig($"{ChebsRecipeConfig.PrefabName}", "RefuelerPylonAllowed", true,
                "Whether making a Refueler Pylon is allowed or not.", plugin.BoolValue, true);
            ChebsRecipeConfig.CraftingCost = plugin.ModConfig($"{ChebsRecipeConfig.PrefabName}", "RefuelerPylonBuildCosts", 
                ChebsRecipeConfig.DefaultRecipe, "Materials needed to build a Refueler Pylon. None or Blank will use Default settings.",
                ChebsRecipeConfig.RecipeValue, true);
            SightRadius = plugin.ModConfig($"{ChebsRecipeConfig.PrefabName}", "RefuelerPylonSightRadius", 30f,
                "How far a Refueler Pylon can reach containers.", plugin.DistanceValue, true);
            RefuelerUpdateInterval = plugin.ModConfig($"{ChebsRecipeConfig.PrefabName}", "RefuelerPylonUpdateInterval", 5f,
                "How long a Refueler Pylon waits between checking containers (lower values may negatively impact performance).",
                plugin.TimeValue, true);
            RefuelerContainerWidth = plugin.ModConfig($"{ChebsRecipeConfig.PrefabName}", "RefuelerPylonContainerWidth", 4,
                "Inventory size = width * height = 4 * 4 = 16.", new AcceptableValueRange<int>(2, 10), true);
            RefuelerContainerHeight = plugin.ModConfig($"{ChebsRecipeConfig.PrefabName}", "RefuelerPylonContainerHeight", 4,
                "Inventory size = width * height = 4 * 4 = 16.", new AcceptableValueRange<int>(4, 20), true);
        }

        private void Awake()
        {
            PieceMask = LayerMask.GetMask("piece");

            Container = GetComponent<Container>();

            Container.m_width = RefuelerContainerWidth.Value;
            Container.m_height = RefuelerContainerHeight.Value;

            StartCoroutine(LookForFurnaces());
        }

        IEnumerator LookForFurnaces()
        {
            yield return new WaitWhile(() => ZInput.instance == null);

            // prevent coroutine from doing its thing while the pylon isn't
            // yet constructed
            Piece piece = GetComponent<Piece>();
            yield return new WaitWhile(() => !piece.IsPlacedByPlayer());

            while (true)
            {
                yield return new WaitForSeconds(RefuelerUpdateInterval.Value);

                GetNearbySmelters().ForEach(ManageSmelter);
            }
        }

        private List<Smelter> GetNearbySmelters()
        {
            // find and return smelters in range

            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position + Vector3.up, SightRadius.Value, PieceMask);
            if (nearbyColliders.Length < 1) return null;

            List<Smelter> result = new List<Smelter>();
            nearbyColliders.ToList().ForEach(collider =>
            {
                Smelter smelter = collider.GetComponentInParent<Smelter>();
                if (smelter != null) { result.Add(smelter); }
            });

            return result;
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

            Inventory inventory = Container.GetInventory();

            if (inventory == null) return;

            void LoadSmelterWithFuel(string fuel)
            {
                while (inventory.CountItems(fuel) > 0)
                {
                    float currentFuel = smelter.GetFuel();
                    if (currentFuel < smelter.m_maxFuel)
                    {
                        smelter.SetFuel(currentFuel + 1);
                        inventory.RemoveItem(fuel, 1);
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
            ItemDrop.ItemData itemData = smelter.FindCookableItem(inventory);
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
                inventory.RemoveItem(itemData, 1);
                smelter.m_nview.InvokeRPC("AddOre", itemData.m_dropPrefab.name);
                smelter.m_addedOreTime = Time.time;
                if (smelter.m_addOreAnimationDuration > 0f)
                {
                    smelter.SetAnimation(true);
                }
            }
        }
    }
}
