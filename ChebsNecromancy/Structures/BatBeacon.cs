using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using ChebsNecromancy.Common;
using UnityEngine;

namespace ChebsNecromancy.Structures
{
    internal class BatBeacon : MonoBehaviour
    {
        public static ConfigEntry<float> SightRadius;
        public static ConfigEntry<float> BatDuration;
        public static ConfigEntry<float> DelayBetweenBats;
        public static ConfigEntry<int> MaxBats;
        protected static List<GameObject> SpawnedBats = new();
        private static float batLastSpawnedAt;

        public static ChebsRecipe ChebsRecipeConfig = new()
        {
            DefaultRecipe = "FineWood:10,Silver:5,Guck:15",
            IconName = "chebgonaz_batbeacon_icon.png",
            PieceTable = "_HammerPieceTable",
            PieceCategory = "Misc",
            PieceName = "$chebgonaz_batbeacon_name",
            PieceDescription = "$chebgonaz_batbeacon_desc",
            PrefabName = "ChebGonaz_BatBeacon.prefab",
        };

        public static void CreateConfigs(BasePlugin plugin)
        {
            ChebsRecipeConfig.Allowed = plugin.ModConfig("BatBeacon", "BatBeaconAllowed", true,
                "Whether making a Spirit Pylon is allowed or not.", plugin.BoolValue, true);
            ChebsRecipeConfig.CraftingCost = plugin.ModConfig("BatBeacon", "BatBeaconBuildCosts", ChebsRecipeConfig.DefaultRecipe, 
                "Materials needed to build the bat beacon. None or Blank will use Default settings.", ChebsRecipeConfig.RecipeValue, true);
            SightRadius = plugin.ModConfig("BatBeacon", "BatBeaconSightRadius",
                30f, "How far a bat beacon can see enemies.", plugin.DistanceValue,
                true);
            BatDuration = plugin.ModConfig("BatBeacon", "BatBeaconGhostDuration",
                30f, "How long a bat persists.", plugin.TimeValue,
                true);
            DelayBetweenBats = plugin.ModConfig("BatBeacon", "BatBeaconDelayBetweenBats",
                .5f, "How long a bat beacon wait before being able to spawn another bat.", plugin.TimeValue,
                true);
            MaxBats = plugin.ModConfig("BatBeacon", "BatBeaconMaxBats",
                15, "The maximum number of bats that a bat beacon can spawn.", plugin.QuantityValue,
                true);
        }

#pragma warning disable IDE0051 // Remove unused private members
        private void Awake()
#pragma warning restore IDE0051 // Remove unused private members
        {
            StartCoroutine(LookForEnemies());
        }

        IEnumerator LookForEnemies()
        {
            yield return new WaitWhile(() => ZInput.instance == null);

            // prevent coroutine from doing its thing while the pylon isn't
            // yet constructed
            Piece piece = GetComponent<Piece>();
            yield return new WaitWhile(() => !piece.IsPlacedByPlayer());

            while (true)
            {
                yield return new WaitForSeconds(2);

                // clear out any dead/destroyed bats
                for (int i = SpawnedBats.Count - 1; i >= 0; i--)
                {
                    if (SpawnedBats[i] == null)
                    {
                        SpawnedBats.RemoveAt(i);
                    }
                }

                if (!EnemiesNearby(out Character characterInRange)) continue;
                
                // spawn up until the limit
                if (SpawnedBats.Count < MaxBats.Value)
                {
                    if (Time.time > batLastSpawnedAt + DelayBetweenBats.Value)
                    {
                        batLastSpawnedAt = Time.time;

                        GameObject friendlyBat = SpawnFriendlyBat();
                        friendlyBat.GetComponent<MonsterAI>().SetTarget(characterInRange);
                        SpawnedBats.Add(friendlyBat);
                    }
                }
            }
        }

        protected bool EnemiesNearby(out Character characterInRange)
        {
            List<Character> charactersInRange = new();
            Character.GetCharactersInRange(
                transform.position,
                SightRadius.Value,
                charactersInRange
                );
            foreach (var character in charactersInRange.Where(character => character != null && character.m_faction != Character.Faction.Players))
            {
                characterInRange = character;
                return true;
            }
            characterInRange = null;
            return false;
        }

        protected GameObject SpawnFriendlyBat()
        {
            int quality = 1;

            string prefabName = "ChebGonaz_Bat";
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Jotunn.Logger.LogError($"spawning {prefabName} failed!");
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
