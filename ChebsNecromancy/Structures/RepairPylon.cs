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
        public static ConfigEntry<float> RepairUpdateInterval, FuelConsumedPerPointOfDamage, RepairWoodWhen, RepairOtherWhen;
        public static ConfigEntry<int> RepairContainerWidth, RepairContainerHeight;
        public static ConfigEntry<string> Fuels;

        private readonly int pieceMask = LayerMask.GetMask("piece");

        private Container _container;
        private Inventory _inventory;
        private float _fuelAccumulator;

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

            FuelConsumedPerPointOfDamage = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "FuelConsumedPerPointOfDamage", .01f,
                "How much fuel is consumed per point of damage. For example at 0.01 it will cost 1 fuel per 100 points of damage healed.",
                null, true);
            
            RepairWoodWhen = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "RepairWoodWhen", .25f,
                "How low a wooden structure's health must drop in order for it to be repaired. Set to 0 to repair regardless of damage.",
                null, true);
            
            RepairOtherWhen = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "RepairOtherWhen", 0f,
                "How low a non-wood structure's health must drop in order for it to be repaired. Set to 0 to repair regardless of damage.",
                null, true);
            
            Fuels = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "Fuels", "Resin,GreydwarfEye,Pukeberries",
                "The items that are consumed as fuel when repairing. Please use a comma-delimited list of prefab names.",
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
                yield return new WaitWhile(() => Player.m_localPlayer != null && Player.m_localPlayer.m_sleeping);

                var piecesInRange = PiecesInRange();
                foreach (var wearNTear in piecesInRange)
                {
                    var healthPercent = wearNTear.GetHealthPercentage();
                    if (RepairDamage(wearNTear))
                    {
                        var player = Player.m_localPlayer;
                        
                        // show repair text if player is near the pylon
                        if (Vector3.Distance(player.transform.position, transform.position) < 5)
                        {
                            Chat.instance.SetNpcText(gameObject, Vector3.up, 5f, 2f, "", 
                                $"Repairing {wearNTear.gameObject.name} ({(healthPercent*100).ToString("0.##")}%)...", false);
                        }
                        
                        // make the hammer sound and puff of smoke etc. if the player is nearby the thing being repaired
                        var distance = Vector3.Distance(player.transform.position, wearNTear.transform.position);
                        if (distance < 20)
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

        private bool RepairDamage(WearNTear wearNTear)
        {
            if (wearNTear.GetHealthPercentage() >= 1f) return false;
            
            if (wearNTear.m_materialType is WearNTear.MaterialType.Wood or WearNTear.MaterialType.HardWood)
            {
                if (RepairWoodWhen.Value != 0.0f && wearNTear.GetHealthPercentage() >= RepairWoodWhen.Value) return false;
            }
            else
            {
                if (RepairOtherWhen.Value != 0.0f && wearNTear.GetHealthPercentage() >= RepairOtherWhen.Value) return false;
            }

            var consumedFuel = ConsumeFuel(wearNTear);
            if (consumedFuel) wearNTear.Repair();
            return consumedFuel;
        }
        
        private int FuelInInventory
        {
            get
            {
                var accumulator = 0;
                foreach (var fuel in Fuels.Value.Split(','))
                {
                    var fuelPrefab = ZNetScene.instance.GetPrefab(fuel);
                    if (fuelPrefab == null) continue;
                    accumulator +=
                        _inventory.CountItems(fuelPrefab.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_name);
                }
                return accumulator;
            }
        }

        private bool ConsumeFuel(int fuelToConsume)
        {
            var consumableFuels = new Dictionary<string, int>();
            var fuelAvailable = 0;
            foreach (var fuel in Fuels.Value.Split(','))
            {
                var fuelPrefab = ZNetScene.instance.GetPrefab(fuel);
                if (fuelPrefab == null) continue;
                var fuelName = fuelPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
                var canConsume = _inventory.CountItems(fuelName);
                consumableFuels[fuelName] = canConsume;
                fuelAvailable += canConsume;

                if (fuelAvailable >= fuelToConsume) break;
            }

            // not enough fuel
            if (fuelAvailable < fuelToConsume) return false;
            
            // enough fuel; consume
            foreach (var key in consumableFuels.Keys)
            {
                var fuel = consumableFuels[key];
                if (fuelToConsume <= fuel)
                {
                    _inventory.RemoveItem(key, fuelToConsume);
                    return true;
                }
                
                fuelToConsume -= fuel;
                _inventory.RemoveItem(key, fuel);
            }

            return true;
        }

        private bool ConsumeFuel(WearNTear wearNTear)
        {
            // first pay any fuel debts
            if (_fuelAccumulator >= 1 && FuelInInventory >= _fuelAccumulator)
            {
                ConsumeFuel((int)_fuelAccumulator);
                _fuelAccumulator -= (int)_fuelAccumulator;
            }
            
            // debts paid - continue on to repair the current damage
            var percentage = wearNTear.GetHealthPercentage();
            if (percentage <= 0) return false;

            var fuelToConsume = (100 - (percentage * 100)) * FuelConsumedPerPointOfDamage.Value;
            // fuel to consume is too small to be currently deducated -> remember the amount and attempt to deduct
            // once it is larger
            if (fuelToConsume < 1) _fuelAccumulator += fuelToConsume;

            if (fuelToConsume > FuelInInventory) return false;

            ConsumeFuel((int)fuelToConsume);

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
                if (!wearAndTear.m_nview.IsValid()) continue;
                result.Add(wearAndTear);
            }

            return result;
        }
    }
}