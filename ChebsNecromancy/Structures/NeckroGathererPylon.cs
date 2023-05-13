using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;

using ChebsNecromancy.Minions;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.Minions;
using ChebsValheimLibrary.Structures;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Structures
{
    internal class NeckroGathererPylon : Structure
    {
        public static ConfigEntry<float> SpawnInterval;
        public static MemoryConfigEntry<string, List<string>> NeckroCost;

        private Container _container;
        private Inventory _inventory;
        
        public new static ChebsRecipe ChebsRecipeConfig = new()
        {
            DefaultRecipe = "Stone:15,NeckTail:25,SurtlingCore:1",
            IconName = "chebgonaz_neckrogathererpylon_icon.png",
            PieceTable = "_HammerPieceTable",
            PieceCategory = "Misc",
            PieceName = "$chebgonaz_neckrogathererpylon_name",
            PieceDescription = "$chebgonaz_neckrogathererpylon_desc",
            PrefabName = "ChebGonaz_NeckroGathererPylon.prefab",
            ObjectName = MethodBase.GetCurrentMethod().DeclaringType.Name
        };

        public static void CreateConfigs(BasePlugin plugin)
        {
            ChebsRecipeConfig.Allowed = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "NeckroGathererPylonAllowed", true,
                "Whether making a the pylon is allowed or not.", plugin.BoolValue, true);

            ChebsRecipeConfig.CraftingCost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "NeckroGathererPylonBuildCosts", 
                ChebsRecipeConfig.DefaultRecipe, 
                "Materials needed to build the pylon. None or Blank will use Default settings. Format: " + ChebsRecipeConfig.RecipeValue, 
                null, true);

            SpawnInterval = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "NeckroGathererSpawnInterval", 60f,
                "How often the pylon will attempt to create a Neckro Gatherer.", plugin.FloatQuantityValue, true);

            var neckroCost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "NeckroGathererCost", "NeckTail:1",
                "The items that are consumed when creating a Neckro Gatherer. Please use a comma-delimited list of prefab names with a : and integer for amount.",
                null, true);
            NeckroCost = new MemoryConfigEntry<string, List<string>>(neckroCost, s => s?.Split(',').ToList());
        }
        
        public new static void UpdateRecipe()
        {
            ChebsRecipeConfig.UpdateRecipe(ChebsRecipeConfig.CraftingCost);
        }

#pragma warning disable IDE0051 // Remove unused private members
        private void Awake()
#pragma warning restore IDE0051 // Remove unused private members
        {
            _container = GetComponent<Container>();
            _inventory = _container.GetInventory();
            StartCoroutine(SpawnNeckros());
        }
        
        private IEnumerator SpawnNeckros()
        {
            yield return new WaitWhile(() => ZInput.instance == null);

            // prevent coroutine from doing its thing while the pylon isn't
            // yet constructed
            Piece piece = GetComponent<Piece>();
            yield return new WaitWhile(() => !piece.IsPlacedByPlayer());

            while (true)
            {
                yield return new WaitForSeconds(SpawnInterval.Value);
                yield return new WaitWhile(() => Player.m_localPlayer == null || Player.m_localPlayer.m_sleeping);

                SpawnNeckro();
            }
        }
        
        private bool CanSpawnNeckro
        {
            get
            {
                var canSpawn = false;
                foreach (var fuel in NeckroCost.Value)
                {
                    var splut = fuel.Split(':');
                    if (splut.Length != 2)
                    {
                        Logger.LogError("Error in config for Neckro Gatherer Costs - please revise.");
                        return false;
                    }
                    
                    var itemRequired = splut[0];
                    if (!int.TryParse(splut[1], out int itemAmountRequired))
                    {
                        Logger.LogError("Error in config for Neckro Gatherer Costs - please revise.");
                        return false;
                    }
                    
                    var requiredItemPrefab = ZNetScene.instance.GetPrefab(itemRequired);
                    if (requiredItemPrefab == null)
                    {
                        Logger.LogError($"Error processing config for Neckro Gatherer Costs: {itemRequired} doesn't exist.");
                        return false;
                    }
                    var amountInInv = _inventory.CountItems(requiredItemPrefab.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_name);
                    if (amountInInv >= itemAmountRequired)
                    {
                        canSpawn = true;
                    }
                        
                }
                return canSpawn;
            }
        }

        private void ConsumeRequirements()
        {
            foreach (var fuel in NeckroCost.Value)
            {
                var splut = fuel.Split(':');
                if (splut.Length != 2)
                {
                    Logger.LogError("Error in config for Neckro Gatherer Costs - please revise.");
                    return;
                }
                    
                var itemRequired = splut[0];
                if (!int.TryParse(splut[1], out int itemAmountRequired))
                {
                    Logger.LogError("Error in config for Neckro Gatherer Costs - please revise.");
                    return;
                }
                    
                var requiredItemPrefab = ZNetScene.instance.GetPrefab(itemRequired);
                if (requiredItemPrefab == null)
                {
                    Logger.LogError($"Error processing config for Neckro Gatherer Costs: {itemRequired} doesn't exist.");
                    return;
                }
                var requiredItemName = requiredItemPrefab.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_name;
                _inventory.RemoveItem(requiredItemName, itemAmountRequired);
            }
        }

        protected void SpawnNeckro()
        {
            // find nearest player and make them the owner
            var player = Player.GetClosestPlayer(transform.position, NeckroGathererMinion.LookRadius.Value);
            if (!player)
            {
                // no master? no neckro
                return;
            }

            if (!CanSpawnNeckro)
            {
                return;
            }
            
            ConsumeRequirements();

            var quality = 1;

            var prefabName = "ChebGonaz_NeckroGatherer";
            var prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Logger.LogError($"spawning {prefabName} failed!");
                return;
            }

            var spawnedChar = Instantiate(
                prefab,
                transform.position + transform.forward * 2f + Vector3.up,
                Quaternion.identity);

            var character = spawnedChar.GetComponent<Character>();
            character.SetLevel(quality);
            
            spawnedChar.GetComponent<NeckroGathererMinion>().UndeadMinionMaster = player.GetPlayerName();
            spawnedChar.AddComponent<FreshMinion>();
        }
    }
}
