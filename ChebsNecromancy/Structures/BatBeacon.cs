using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.Structures;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Structures
{
    internal class BatBeacon : Structure
    {
        public static ConfigEntry<float> SightRadius;
        public static ConfigEntry<float> BatDuration;
        public static ConfigEntry<float> DelayBetweenBats;
        public static ConfigEntry<int> MaxBats;

        protected List<GameObject> SpawnedBats = new();
        private float batLastSpawnedAt;

        public new static ChebsRecipe ChebsRecipeConfig = new()
        {
            DefaultRecipe = "FineWood:10,Silver:5,Guck:15",
            IconName = "chebgonaz_batpylon_icon.png",
            PieceTable = "_HammerPieceTable",
            PieceCategory = "Misc",
            PieceName = "$chebgonaz_batbeacon_name",
            PieceDescription = "$chebgonaz_batbeacon_desc",
            PrefabName = "ChebGonaz_BatBeacon.prefab",
            ObjectName = MethodBase.GetCurrentMethod().DeclaringType.Name
        };

        public new static void UpdateRecipe()
        {
            ChebsRecipeConfig.UpdateRecipe(ChebsRecipeConfig.CraftingCost);
        }

        public static void CreateConfigs(BasePlugin plugin)
        {
            ChebsRecipeConfig.Allowed = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "BatBeaconAllowed", true,
                "Whether making a Spirit Pylon is allowed or not.", plugin.BoolValue, true);

            ChebsRecipeConfig.CraftingCost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "BatBeaconBuildCosts",
                ChebsRecipeConfig.DefaultRecipe,
                "Materials needed to build the bat beacon. None or Blank will use Default settings. Format: " +
                ChebsRecipeConfig.RecipeValue,
                null, true);

            SightRadius = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "BatBeaconSightRadius",
                30f, "How far a bat beacon can see enemies.", plugin.FloatQuantityValue,
                true);

            BatDuration = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "BatBeaconGhostDuration",
                30f, "How long a bat persists.", plugin.FloatQuantityValue,
                true);

            DelayBetweenBats = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "BatBeaconDelayBetweenBats",
                .5f, "How long a bat beacon wait before being able to spawn another bat.", plugin.FloatQuantityValue,
                true);

            MaxBats = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "BatBeaconMaxBats",
                15, "The maximum number of bats that a bat beacon can spawn.", plugin.IntQuantityValue,
                true);
        }

        private void Awake()
        {
            StartCoroutine(LookForEnemies());
        }

        IEnumerator LookForEnemies()
        {
            // prevent coroutine from doing its thing while the pylon isn't
            // yet constructed
            var piece = GetComponent<Piece>();
            yield return new WaitWhile(() => !piece.IsPlacedByPlayer());

            while (true)
            {
                yield return new WaitForSeconds(2);

                if (!piece.m_nview.IsOwner()) continue;

                // clear out any dead/destroyed bats
                for (var i = SpawnedBats.Count - 1; i >= 0; i--)
                {
                    if (SpawnedBats[i] == null)
                    {
                        SpawnedBats.RemoveAt(i);
                    }
                }

                if (!EnemiesNearby(out var characterInRange, SightRadius.Value)) continue;

                // spawn up until the limit
                if (SpawnedBats.Count < MaxBats.Value)
                {
                    if (Time.time > batLastSpawnedAt + DelayBetweenBats.Value)
                    {
                        batLastSpawnedAt = Time.time;

                        var friendlyBat = SpawnFriendlyBat();
                        friendlyBat.GetComponent<MonsterAI>().SetTarget(characterInRange);
                        SpawnedBats.Add(friendlyBat);
                    }
                }
            }
        }

        protected GameObject SpawnFriendlyBat()
        {
            var quality = 1;

            var prefabName = "ChebGonaz_Bat";
            var prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Logger.LogError($"spawning {prefabName} failed!");
                return null;
            }

            var spawnedChar = Instantiate(
                prefab,
                transform.position + transform.forward * 2f + Vector3.up,
                Quaternion.identity);

            var character = spawnedChar.GetComponent<Character>();
            character.SetLevel(quality);

            return spawnedChar;
        }
    }
}