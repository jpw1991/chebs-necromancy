using BepInEx;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Jotunn.Configs;
using Jotunn.Managers;
using BepInEx.Configuration;


namespace FriendlySkeletonWand
{
    internal class SpiritPylon : MonoBehaviour
    {
        public static ConfigEntry<bool> allowed;
        public static ConfigEntry<float> sightRadius;
        public static ConfigEntry<float> ghostDuration;
        public static ConfigEntry<float> delayBetweenGhosts;
        public static ConfigEntry<int> maxGhosts;

        public static string PrefabName = "ChebGonaz_SpiritPylon";
        public static string PieceTable = "Hammer";
        public static string IconName = "chebgonaz_spiritpylon_icon.png";
        protected List<GameObject> spawnedGhosts = new List<GameObject>();

        private float ghostLastSpawnedAt;

        public static RequirementConfig[] GetRequirements()
        {
            return new RequirementConfig[]
            {
                new RequirementConfig("Stone", 15, 0, true),
                new RequirementConfig("Wood", 15, 0, true),
                new RequirementConfig("BoneFragments", 15, 0, true),
                new RequirementConfig("SurtlingCore", 1, 0, true),
            };
        }

        public static void CreateConfigs(BaseUnityPlugin plugin)
        {
            allowed = plugin.Config.Bind("Server config", "SpiritPylonAllowed",
                true, new ConfigDescription("Whether making a Spirit Pylon is allowed or not.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            sightRadius = plugin.Config.Bind("Server config", "SpiritPylonSightRadius",
                30f, new ConfigDescription("How far a Spirit Pylon can see enemies.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            ghostDuration = plugin.Config.Bind("Server config", "SpiritPylonGhostDuration",
                30f, new ConfigDescription("How long a Spirit Pylon's ghost persists.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            delayBetweenGhosts = plugin.Config.Bind("Server config", "SpiritPylonDelayBetweenGhosts",
                5f, new ConfigDescription("How long a Spirit Pylon must wait before being able to spawn another ghost.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            maxGhosts = plugin.Config.Bind("Server config", "SpiritPylonMaxGhosts",
                3, new ConfigDescription("The maximum number of ghosts that a Spirit Pylon can spawn.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
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
                for (int i=spawnedGhosts.Count-1; i>=0; i--)
                {
                    if (spawnedGhosts[i] == null)
                    {
                        spawnedGhosts.RemoveAt(i);
                    }
                }

                if (Player.m_localPlayer != null)
                {
                    if (EnemiesNearby(out Character characterInRange))
                    {
                        // spawn ghosts up until the limit
                        if (spawnedGhosts.Count < maxGhosts.Value)
                        {
                            if (Time.time > ghostLastSpawnedAt + delayBetweenGhosts.Value)
                            {
                                ghostLastSpawnedAt = Time.time;

                                GameObject friendlyGhost = SpawnFriendlyGhost();
                                friendlyGhost.GetComponent<MonsterAI>().SetTarget(characterInRange);
                                spawnedGhosts.Add(friendlyGhost);
                            }
                        }
                    }  
                }
            }
        }

        protected bool EnemiesNearby(out Character characterInRange)
        {
            List<Character> charactersInRange = new List<Character>();
            Character.GetCharactersInRange(
                transform.position,
                sightRadius.Value,
                charactersInRange
                );
            foreach (Character character in charactersInRange)
            {
                if (character != null && character.m_faction != Character.Faction.Players)
                {
                    characterInRange = character;
                    return true;
                }
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
                Jotunn.Logger.LogError($"SpawnFriendlyGhost: spawning {prefabName} failed!");
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
