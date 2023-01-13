using BepInEx;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Jotunn.Configs;
using BepInEx.Configuration;
using System.Linq;
using Jotunn.Entities;

namespace FriendlySkeletonWand
{
    internal class RefuelerPylon : MonoBehaviour
    {
        public static ConfigEntry<bool> allowed;
        public static ConfigEntry<string> craftingCost;
        public static ConfigEntry<float> sightRadius;
        public static ConfigEntry<float> refuelerUpdateInterval;
        public static ConfigEntry<int> refuelerContainerWidth, refuelerContainerHeight;

        public static string PrefabName = "ChebGonaz_RefuelerPylon.prefab";
        public static string PieceTable = "Hammer";
        public static string IconName = "chebgonaz_refuelerpylon_icon.png";

        protected const string DefaultRecipe = "Stone:15,Coal:15,BoneFragments:15,SurtlingCore:1";

        protected int pieceMask;
        protected Container container;

        public static void CreateConfigs(BaseUnityPlugin plugin)
        {
            allowed = plugin.Config.Bind($"{PrefabName} (Server Synced)", "RefuelerPylonAllowed",
                true, new ConfigDescription("Whether making a Refueler Pylon is allowed or not.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            craftingCost = plugin.Config.Bind($"{PrefabName} (Server Synced)", "RefuelerPylonBuildCosts",
                DefaultRecipe, new ConfigDescription("Materials needed to build a Refueler Pylon. None or Blank will use Default settings.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            sightRadius = plugin.Config.Bind($"{PrefabName} (Server Synced)", "RefuelerPylonSightRadius",
                30f, new ConfigDescription("How far a Refueler Pylon can reach containers.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            refuelerUpdateInterval = plugin.Config.Bind($"{PrefabName} (Server Synced)", "RefuelerPylonUpdateInterval",
                5f, new ConfigDescription("How long a Refueler Pylon waits between checking containers (lower values may negatively impact performance).", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            refuelerContainerWidth = plugin.Config.Bind($"{PrefabName} (Server Synced)", "RefuelerPylonContainerWidth",
                4, new ConfigDescription("Inventory size = width * height = 4 * 4 = 16.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            refuelerContainerHeight = plugin.Config.Bind($"{PrefabName} (Server Synced)", "RefuelerPylonContainerHeight",
                4, new ConfigDescription("Inventory size = width * height = 4 * 4 = 16.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        private void Awake()
        {
            pieceMask = LayerMask.GetMask(new string[1] { "piece" });

            container = GetComponent<Container>();

            container.m_width = refuelerContainerWidth.Value;
            container.m_height = refuelerContainerHeight.Value;

            StartCoroutine(LookForFurnaces());
        }

        public CustomPiece GetCustomPieceFromPrefab(GameObject prefab, Sprite icon)
        {
            PieceConfig config = new PieceConfig();
            config.Name = "$chebgonaz_refuelerpylon_name";
            config.Description = "$chebgonaz_refuelerpylon_desc";

            if (allowed.Value)
            {
                if (string.IsNullOrEmpty(craftingCost.Value))
                {
                    craftingCost.Value = DefaultRecipe;
                }
                // set recipe requirements
                SetRecipeReqs(config, craftingCost);
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
                Jotunn.Logger.LogError($"AddCustomPieces: {PrefabName}'s CustomPiece is null!");
                return null;
            }
            if (customPiece.PiecePrefab == null)
            {
                Jotunn.Logger.LogError($"AddCustomPieces: {PrefabName}'s PiecePrefab is null!");
                return null;
            }

            return customPiece;
        }


        public void SetRecipeReqs(PieceConfig config, ConfigEntry<string> craftingCost)
        {
            // function to add a single material to the recipe
            void addMaterial(string material)
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
                    addMaterial(material);
                }
            }
            else
            {
                addMaterial(craftingCost.Value);
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
                yield return new WaitForSeconds(refuelerUpdateInterval.Value);

                GetNearbySmelters().ForEach(ManageSmelter);
            }
        }

        private List<Smelter> GetNearbySmelters()
        {
            // find and return smelters in range

            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position + Vector3.up, sightRadius.Value, pieceMask);
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

            Inventory inventory = container.GetInventory();

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
                    Jotunn.Logger.LogInfo($"Refueler pylon can't smelt item {itemData.m_dropPrefab.name}");
                    return;
                }
                if (smelter.GetQueueSize() >= smelter.m_maxOre)
                {
                    Jotunn.Logger.LogInfo($"Refueler pylon can't smelt item {itemData.m_dropPrefab.name}, queue is full");
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
