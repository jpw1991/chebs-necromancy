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
    internal class SpiritPylon : Structure
    {
        public static ConfigEntry<float> SightRadius;
        public static ConfigEntry<float> GhostDuration;
        public static ConfigEntry<float> DelayBetweenGhosts;
        public static ConfigEntry<int> MaxGhosts;

        protected List<GameObject> SpawnedGhosts = new();
        private float ghostLastSpawnedAt;

        public new static ChebsRecipe ChebsRecipeConfig = new()
        {
            DefaultRecipe = "Stone:15,Wood:15,BoneFragments:15,SurtlingCore:1",
            IconName = "chebgonaz_spiritpylon_icon.png",
            PieceTable = "_HammerPieceTable",
            PieceCategory = "Misc",
            PieceName = "$chebgonaz_spiritpylon_name",
            PieceDescription = "$chebgonaz_spiritpylon_desc",
            PrefabName = "ChebGonaz_SpiritPylon.prefab",
            ObjectName = MethodBase.GetCurrentMethod().DeclaringType.Name
        };

        public new static void UpdateRecipe()
        {
            ChebsRecipeConfig.UpdateRecipe(ChebsRecipeConfig.CraftingCost);
        }

        public static void CreateConfigs(BasePlugin plugin)
        {
            ChebsRecipeConfig.Allowed = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "SpiritPylonAllowed", true,
                "Whether making a Spirit Pylon is allowed or not.", plugin.BoolValue, true);

            ChebsRecipeConfig.CraftingCost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "SpiritPylonBuildCosts",
                ChebsRecipeConfig.DefaultRecipe,
                "Materials needed to build Spirit Pylon. None or Blank will use Default settings. Format: " +
                ChebsRecipeConfig.RecipeValue,
                null, true);

            SightRadius = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "SpiritPylonSightRadius", 30f,
                "How far a Spirit Pylon can see enemies.", plugin.FloatQuantityValue, true);

            GhostDuration = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "SpiritPylonGhostDuration", 30f,
                "How long a Spirit Pylon's ghost persists.", plugin.FloatQuantityValue, true);

            DelayBetweenGhosts = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "SpiritPylonDelayBetweenGhosts", 5f,
                "How long a Spirit Pylon must wait before being able to spawn another ghost.",
                plugin.FloatQuantityValue, true);

            MaxGhosts = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "SpiritPylonMaxGhosts", 3,
                "The maximum number of ghosts that a Spirit Pylon can spawn.", plugin.IntQuantityValue, true);
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

                // clear out any dead/destroyed ghosts
                for (var i = SpawnedGhosts.Count - 1; i >= 0; i--)
                {
                    if (SpawnedGhosts[i] == null)
                    {
                        SpawnedGhosts.RemoveAt(i);
                    }
                }

                if (!EnemiesNearby(out var characterInRange, SightRadius.Value)) continue;

                // spawn ghosts up until the limit
                if (SpawnedGhosts.Count < MaxGhosts.Value)
                {
                    if (Time.time > ghostLastSpawnedAt + DelayBetweenGhosts.Value)
                    {
                        ghostLastSpawnedAt = Time.time;

                        var friendlyGhost = SpawnFriendlyGhost();
                        friendlyGhost.GetComponent<MonsterAI>().SetTarget(characterInRange);
                        SpawnedGhosts.Add(friendlyGhost);
                    }
                }
            }
        }

        protected GameObject SpawnFriendlyGhost()
        {
            var quality = 1;

            var prefabName = "ChebGonaz_SpiritPylonGhost";
            var prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Logger.LogError($"SpawnFriendlyGhost: spawning {prefabName} failed!");
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