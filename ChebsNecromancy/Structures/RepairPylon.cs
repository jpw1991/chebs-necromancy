using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using ChebsNecromancy.Common;
using UnityEngine;

namespace ChebsNecromancy.Structures
{
    internal class RepairPylon : Structure
    {
        public static ConfigEntry<float> SightRadius;
        public static ConfigEntry<float> RepairUpdateInterval, ResinConsumedPerPointOfDamage;
        public static ConfigEntry<int> RepairContainerWidth, RepairContainerHeight;

        private readonly int pieceMask = LayerMask.GetMask("piece");

        private Container _container;
        private Inventory _inventory;

        public new static ChebsRecipe ChebsRecipeConfig = new()
        {
            DefaultRecipe = "Stone:15,GreydwarfEye:50,SurtlingCore:1",
            IconName = "chebgonaz_repairpylon_icon.png",
            PieceTable = "_HammerPieceTable",
            PieceCategory = "Misc",
            PieceName = "$chebgonaz_repairpylon_name",
            PieceDescription = "$chebgonaz_repairpylon_desc",
            PrefabName = "ChebGonaz_RepairPylon.prefab",
            ObjectName = MethodBase.GetCurrentMethod().DeclaringType.Name
        };

        public new static void UpdateRecipe()
        {
            ChebsRecipeConfig.UpdateRecipe(ChebsRecipeConfig.CraftingCost);
        }
        
        public static void CreateConfigs(BasePlugin plugin)
        {
            ChebsRecipeConfig.Allowed = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "RepairPylonAllowed", true,
                "Whether making a Repair Pylon is allowed or not.", plugin.BoolValue, true);

            ChebsRecipeConfig.CraftingCost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "RepairPylonBuildCosts",
                ChebsRecipeConfig.DefaultRecipe,
                "Materials needed to build a Repair Pylon. None or Blank will use Default settings. Format: " +
                ChebsRecipeConfig.RecipeValue,
                null, true);

            SightRadius = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "RepairPylonSightRadius", 30f,
                "How far a Repair Pylon can reach containers.", plugin.FloatQuantityValue, true);

            RepairUpdateInterval = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "RepairPylonUpdateInterval", 5f,
                "How long a Repair Pylon waits between checking containers (lower values may negatively impact performance).",
                plugin.FloatQuantityValue, true);

            RepairContainerWidth = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "RepairPylonContainerWidth", 4,
                "Inventory size = width * height = 4 * 4 = 16.", new AcceptableValueRange<int>(2, 10), true);

            RepairContainerHeight = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "RepairPylonContainerHeight", 4,
                "Inventory size = width * height = 4 * 4 = 16.", new AcceptableValueRange<int>(4, 20), true);

            ResinConsumedPerPointOfDamage = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "ResinConsumedPerPointOfDamage", .25f,
                "How low a structure's health must drop in order for it to be repaired.",
                null, true);
        }
        
        private void Awake()
        {
            _container = GetComponent<Container>();
            _inventory = _container.GetInventory();

            _container.m_width = RepairContainerWidth.Value;
            _container.m_height = RepairContainerHeight.Value;

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
                yield return new WaitForSeconds(RepairUpdateInterval.Value);

                var piecesInRange = PiecesInRange();
                foreach (var wearNTear in piecesInRange)
                {
                    var healthPercent = wearNTear.GetHealthPercentage();
                    if (healthPercent < 1
                        && ConsumeResin(wearNTear))
                    {
                        Chat.instance.SetNpcText(gameObject, Vector3.up, 5f, 2f, "", 
                            $"Repairing {wearNTear.gameObject.name} ({(healthPercent*100).ToString("0.##")}%)...", false);
                        wearNTear.Repair();
                        // make the hammer sound and puff of smoke etc. if the player is nearby
                        Player player = Player.m_localPlayer;
                        if (Vector3.Distance(player.transform.position, wearNTear.transform.position) < 20)
                        {
                            var localPiece = wearNTear.m_piece;
                            if (localPiece is not null)
                            {
                                var localPieceTransform = localPiece.transform;
                                localPiece.m_placeEffect.Create(localPieceTransform.position,
                                    localPieceTransform.rotation);
                            }
                        }
                    }

                    yield return new WaitForSeconds(1);
                }
            }
        }

        private bool ConsumeResin(WearNTear wearNTear)
        {
            var resinInInventory = _inventory.CountItems("$item_resin");

            var percentage = wearNTear.GetHealthPercentage();
            if (percentage <= 0) return false;

            var resinToConsume = (100 - (percentage * 100)) * ResinConsumedPerPointOfDamage.Value;
            if (resinToConsume < 1) resinToConsume = 1;

            if (resinToConsume > resinInInventory) return false;

            _inventory.RemoveItem("$item_resin", (int)resinToConsume);
            
            return true;
        }

        private List<WearNTear> PiecesInRange()
        {
            var nearbyColliders = Physics.OverlapSphere(transform.position + Vector3.up, SightRadius.Value, pieceMask);
            if (nearbyColliders.Length < 1) return null;

            var result = new List<WearNTear>();
            foreach (var nearbyCollider in nearbyColliders)
            {
                var wearAndTear = nearbyCollider.GetComponentInParent<WearNTear>();
                if (wearAndTear == null) continue;
                result.Add(wearAndTear);
            }

            return result;
        }
    }
}