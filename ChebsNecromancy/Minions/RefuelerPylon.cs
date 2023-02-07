using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Minions
{
    internal class RefuelerPylon : MonoBehaviour
    {
        public static ConfigEntry<bool> Allowed;
        public static ConfigEntry<string> CraftingCost;
        public static ConfigEntry<float> SightRadius;
        public static ConfigEntry<float> RefuelerUpdateInterval;
        public static ConfigEntry<int> RefuelerContainerWidth, RefuelerContainerHeight;

        public const string PrefabName = "ChebGonaz_RefuelerPylon.prefab";
        public const string PieceTable = "Hammer";
        public const string IconName = "chebgonaz_refuelerpylon_icon.png";

        protected const string DefaultRecipe = "Stone:15,Coal:15,BoneFragments:15,SurtlingCore:1";

        protected int PieceMask;
        protected Container Container;

        public static void CreateConfigs(BaseUnityPlugin plugin)
        {
            Allowed = plugin.Config.Bind($"{PrefabName} (Server Synced)", "RefuelerPylonAllowed",
                true, new ConfigDescription("Whether making a Refueler Pylon is allowed or not.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingCost = plugin.Config.Bind($"{PrefabName} (Server Synced)", "RefuelerPylonBuildCosts",
                DefaultRecipe, new ConfigDescription("Materials needed to build a Refueler Pylon. None or Blank will use Default settings.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SightRadius = plugin.Config.Bind($"{PrefabName} (Server Synced)", "RefuelerPylonSightRadius",
                30f, new ConfigDescription("How far a Refueler Pylon can reach containers.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            RefuelerUpdateInterval = plugin.Config.Bind($"{PrefabName} (Server Synced)", "RefuelerPylonUpdateInterval",
                5f, new ConfigDescription("How long a Refueler Pylon waits between checking containers (lower values may negatively impact performance).", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            RefuelerContainerWidth = plugin.Config.Bind($"{PrefabName} (Server Synced)", "RefuelerPylonContainerWidth",
                4, new ConfigDescription("Inventory size = width * height = 4 * 4 = 16.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            RefuelerContainerHeight = plugin.Config.Bind($"{PrefabName} (Server Synced)", "RefuelerPylonContainerHeight",
                4, new ConfigDescription("Inventory size = width * height = 4 * 4 = 16.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        private void Awake()
        {
            PieceMask = LayerMask.GetMask("piece");

            Container = GetComponent<Container>();

            Container.m_width = RefuelerContainerWidth.Value;
            Container.m_height = RefuelerContainerHeight.Value;

            StartCoroutine(LookForFurnaces());
        }

        public CustomPiece GetCustomPieceFromPrefab(GameObject prefab, Sprite icon)
        {
            PieceConfig config = new PieceConfig();
            config.Name = "$chebgonaz_refuelerpylon_name";
            config.Description = "$chebgonaz_refuelerpylon_desc";

            if (Allowed.Value)
            {
                if (string.IsNullOrEmpty(CraftingCost.Value))
                {
                    CraftingCost.Value = DefaultRecipe;
                }
                // set recipe requirements
                SetRecipeReqs(config, CraftingCost);
            }
            else
            {
                config.Enabled = false;
            }

            config.Icon = icon;
            config.PieceTable = "_HammerPieceTable";
            config.Category = "Misc";

            CustomPiece customPiece = new CustomPiece(prefab, false, config);
            if (customPiece == null)
            {
                Logger.LogError($"AddCustomPieces: {PrefabName}'s CustomPiece is null!");
                return null;
            }
            if (customPiece.PiecePrefab == null)
            {
                Logger.LogError($"AddCustomPieces: {PrefabName}'s PiecePrefab is null!");
                return null;
            }

            return customPiece;
        }


        public void SetRecipeReqs(PieceConfig config, ConfigEntry<string> craftingCost)
        {
            // function to add a single material to the recipe
            void AddMaterial(string material)
            {
                string[] materialSplit = material.Split(':');
                string materialName = materialSplit[0];
                int materialAmount = int.Parse(materialSplit[1]);
                config.AddRequirement(new RequirementConfig(materialName, materialAmount, 0, true));
            }

            // build the recipe. material config format ex: Wood:5,Stone:1,Resin:1
            if (craftingCost.Value.Contains(','))
            {
                string[] materialList = craftingCost.Value.Split(',');

                foreach (string material in materialList)
                {
                    AddMaterial(material);
                }
            }
            else
            {
                AddMaterial(craftingCost.Value);
            }
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

                Tuple<List<Smelter>, List<Fireplace>> tuple = GetNearbySmeltersAndFireplaces();

                List<Smelter> smelters = tuple.Item1;
                smelters.ForEach(ManageSmelter);
                List<Fireplace> fireplaces = tuple.Item2;
                fireplaces.ForEach(ManageFireplace);
                
            }
        }

        private Tuple<List<Smelter>, List<Fireplace>> GetNearbySmeltersAndFireplaces()
        {
            // find and return smelters and fireplaces in range

            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position + Vector3.up, SightRadius.Value, PieceMask);
            if (nearbyColliders.Length < 1) return null;

            List<Smelter> smelters = new();
            List<Fireplace> fireplaces = new();
            nearbyColliders.ToList().ForEach(nearbyCollider =>
            {
                Smelter smelter = nearbyCollider.GetComponentInParent<Smelter>();
                if (smelter != null) smelters.Add(smelter);
                Fireplace fireplace = nearbyCollider.GetComponentInParent<Fireplace>();
                if (fireplace != null) fireplaces.Add(fireplace);
            });

            return new Tuple<List<Smelter>, List<Fireplace>>(smelters, fireplaces);
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

        private void ManageFireplace(Fireplace fireplace)
        {
            float currentFuel = fireplace.m_nview.GetZDO().GetFloat("fuel");
            // fuel is always an incomplete number like 5.98/6.00 because the moment you add the fuel
            // it begins decreasing. So minus 1 from the max so we only add fuel if it is something like
            // 4.98/6.00
            if (currentFuel >= fireplace.m_maxFuel-1) return;
            
            Inventory inventory = Container.GetInventory();

            if (inventory == null) return;

            if (inventory.HaveItem(fireplace.m_fuelItem.m_itemData.m_shared.m_name))
            {
                fireplace.m_nview.InvokeRPC("AddFuel");
                inventory.RemoveItem(fireplace.m_fuelItem.m_itemData.m_shared.m_name, 1);
            }
        }
    }
}
