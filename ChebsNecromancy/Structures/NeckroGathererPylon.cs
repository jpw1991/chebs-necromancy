﻿using System.Collections;
using BepInEx.Configuration;
using ChebsNecromancy.Common;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Structures
{
    internal class NeckroGathererPylon : MonoBehaviour
    {
        public static ConfigEntry<float> SpawnInterval;
        public static ConfigEntry<int> NeckTailsConsumedPerSpawn;
        protected static Container Container;
        
        public static ChebsRecipe ChebsRecipeConfig = new()
        {
            DefaultRecipe = "Stone:15,NeckTail:25,SurtlingCore:1",
            IconName = "chebgonaz_neckrogathererpylon_icon.png",
            PieceTable = "_HammerPieceTable",
            PieceCategory = "Misc",
            PieceName = "$chebgonaz_neckrogathererpylon_name",
            PieceDescription = "$chebgonaz_neckrogathererpylon_desc",
            PrefabName = "ChebGonaz_NeckroGathererPylon.prefab",
        };

        public static void CreateConfigs(BasePlugin plugin)
        {
            ChebsRecipeConfig.Allowed = plugin.ModConfig("NeckroGathererPylon", "NeckroGathererPylonAllowed", true,
                "Whether making a the pylon is allowed or not.", plugin.BoolValue, true);

            ChebsRecipeConfig.CraftingCost = plugin.ModConfig("NeckroGathererPylon", "NeckroGathererPylonBuildCosts", ChebsRecipeConfig.DefaultRecipe,
                "Materials needed to build the pylon. None or Blank will use Default settings.", ChebsRecipeConfig.RecipeValue, true);

            SpawnInterval = plugin.ModConfig("NeckroGathererPylon", "NeckroGathererSpawnInterval", 60f,
                "How often the pylon will attempt to create a Neckro Gatherer.", plugin.TimeValue, true);

            NeckTailsConsumedPerSpawn = plugin.ModConfig("NeckroGathererPylon", "NeckroGathererCreationCost", 1,
                "How many Neck Tails get consumed when creating a Neckro Gatherer.", plugin.QuantityValue, true);
        }

        private void Awake()
        {
            Container = GetComponent<Container>();
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

                SpawnNeckro();
            }
        }

        protected GameObject SpawnNeckro()
        {
            int neckTailsInInventory = Container.GetInventory().CountItems("$item_necktail");
            if (neckTailsInInventory < NeckTailsConsumedPerSpawn.Value) return null;

            Container.GetInventory().RemoveItem("$item_necktail", NeckTailsConsumedPerSpawn.Value);

            int quality = 1;

            string prefabName = "ChebGonaz_NeckroGatherer";
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Logger.LogError($"spawning {prefabName} failed!");
                return null;
            }

            GameObject spawnedChar = Instantiate(
                prefab,
                transform.position + transform.forward * 2f + Vector3.up,
                Quaternion.identity);

            Character character = spawnedChar.GetComponent<Character>();
            character.SetLevel(quality);

            return spawnedChar;
        }
    }
}
