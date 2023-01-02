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

        public static string PrefabName = "ChebGonaz_SpiritPylon";
        public static string PieceTable = "Hammer";
        public static string IconName = "chebgonaz_spiritpylon_icon.png";
        protected List<GameObject> spawnedGhosts = new List<GameObject>();

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
            allowed = plugin.Config.Bind("Client config", "SpiritPylonAllowed",
                true, new ConfigDescription("Whether making a Spirit Pylon is allowed or not."));
            sightRadius = plugin.Config.Bind("Client config", "SpiritPylonSightRadius",
                30f, new ConfigDescription("How far a Spirit Pylon can see enemies."));
            ghostDuration = plugin.Config.Bind("Client config", "SpiritPylonGhostDuration",
                30f, new ConfigDescription("How long a Spirit Pylon's ghost persists."));
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
                    float playerNecromancyLevel = 
                        Player.m_localPlayer.GetSkillLevel(
                            SkillManager.Instance.GetSkill(BasePlugin.necromancySkillIdentifier)
                                .m_skill);
                    int amount = playerNecromancyLevel <= 19 
                        ? 1 
                        : (int)playerNecromancyLevel / 10;

                    if (EnemiesNearby(out Character characterInRange))
                    {
                        // spawn ghosts up until the limit
                        if (spawnedGhosts.Count < amount)
                        {
                            GameObject friendlyGhost = SpawnFriendlyGhost(playerNecromancyLevel);
                            friendlyGhost.GetComponent<MonsterAI>().SetTarget(characterInRange);
                            spawnedGhosts.Add(friendlyGhost);
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

        protected GameObject SpawnFriendlyGhost(float playerNecromancyLevel)
        {
            int quality = 1;
            if (playerNecromancyLevel >= 70) { quality = 3; }
            else if (playerNecromancyLevel >= 35) { quality = 2; }

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

            // add a self-destruct to it
            //spawnedChar.AddComponent<KillAfterPeriod>();
            //Jotunn.Logger.LogInfo("KillAfterPeriod component added");

            Character character = spawnedChar.GetComponent<Character>();
            character.SetLevel(quality);

            return spawnedChar;
        }
    }
}
