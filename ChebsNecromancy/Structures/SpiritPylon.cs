using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using ChebsNecromancy.Common;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Structures
{
    internal class SpiritPylon : MonoBehaviour
    {
        public static ConfigEntry<float> SightRadius;
        public static ConfigEntry<float> GhostDuration;
        public static ConfigEntry<float> DelayBetweenGhosts;
        public static ConfigEntry<int> MaxGhosts;

        protected static List<GameObject> SpawnedGhosts = new List<GameObject>();
        private static float ghostLastSpawnedAt;

        public static ChebsRecipe ChebsRecipeConfig = new()
        {
            DefaultRecipe = "Stone:15,Wood:15,BoneFragments:15,SurtlingCore:1",
            IconName = "chebgonaz_spiritpylon_icon.png",
            PieceTable = "_HammerPieceTable",
            PieceCategory = "Misc",
            PieceName = "$chebgonaz_spiritpylon_name",
            PieceDescription = "$chebgonaz_spiritpylon_desc",
            PrefabName = "ChebGonaz_SpiritPylon.prefab",
        };

        public static void CreateConfigs(BasePlugin plugin)
        {            
            ChebsRecipeConfig.Allowed = plugin.ModConfig("SpiritPylon", "SpiritPylonAllowed", true,
                "Whether making a Spirit Pylon is allowed or not.", plugin.DistanceValue, true);
            ChebsRecipeConfig.CraftingCost = plugin.ModConfig("SpiritPylon", "Spirit Pylon Build Costs",
                ChebsRecipeConfig.DefaultRecipe, 
                "Materials needed to build Spirit Pylon. None or Blank will use Default settings.",
                ChebsRecipeConfig.RecipeValue, true);
            SightRadius = plugin.ModConfig("SpiritPylon", "SpiritPylonSightRadius", 30f,
                "How far a Spirit Pylon can see enemies.", plugin.QuantityValue, true);
            GhostDuration = plugin.ModConfig("SpiritPylon", "SpiritPylonGhostDuration", 30f,
                "How long a Spirit Pylon's ghost persists.", plugin.TimeValue, true);
            DelayBetweenGhosts = plugin.ModConfig("SpiritPylon", "SpiritPylonDelayBetweenGhosts", 5f,
                "How long a Spirit Pylon must wait before being able to spawn another ghost.",
                plugin.TimeValue, true);
            MaxGhosts = plugin.ModConfig("SpiritPylon", "SpiritPylonMaxGhosts", 3,
                "The maximum number of ghosts that a Spirit Pylon can spawn.", plugin.QuantityValue, true);
        }

        private void Awake()
        {
            StartCoroutine(LookForEnemies());
        }

        IEnumerator LookForEnemies()
        {
            while (ZInput.instance == null)
            {
                yield return new WaitForSeconds(2);
            }

            // prevent coroutine from doing its thing while the pylon isn't
            // yet constructed
            Piece piece = GetComponent<Piece>();
            while (!piece.IsPlacedByPlayer())
            {
                //Jotunn.Logger.LogInfo("Waiting for player to place pylon...");
                yield return new WaitForSeconds(2);
            }

            while (true)
            {
                yield return new WaitForSeconds(2);

                // clear out any dead/destroyed ghosts
                for (int i=SpawnedGhosts.Count-1; i>=0; i--)
                {
                    if (SpawnedGhosts[i] == null)
                    {
                        SpawnedGhosts.RemoveAt(i);
                    }
                }

                if (Player.m_localPlayer == null) continue;
                if (!EnemiesNearby(out Character characterInRange)) continue;
                
                // spawn ghosts up until the limit
                if (SpawnedGhosts.Count < MaxGhosts.Value)
                {
                    if (Time.time > ghostLastSpawnedAt + DelayBetweenGhosts.Value)
                    {
                        ghostLastSpawnedAt = Time.time;

                        GameObject friendlyGhost = SpawnFriendlyGhost();
                        friendlyGhost.GetComponent<MonsterAI>().SetTarget(characterInRange);
                        SpawnedGhosts.Add(friendlyGhost);
                    }
                }
            }
        }

        protected bool EnemiesNearby(out Character characterInRange)
        {
            List<Character> charactersInRange = new List<Character>();
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

        protected GameObject SpawnFriendlyGhost()
        {
            int quality = 1;

            string prefabName = "ChebGonaz_SpiritPylonGhost";
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Logger.LogError($"SpawnFriendlyGhost: spawning {prefabName} failed!");
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
