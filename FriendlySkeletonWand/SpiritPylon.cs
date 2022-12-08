using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Jotunn.Configs;
using Jotunn.Managers;
namespace FriendlySkeletonWand
{
    internal class SpiritPylon : MonoBehaviour
    {
        public float sightRadius = 15;
        public static string PrefabName = "ChebGonaz_SpiritPylon";
        public static string PieceTable = "Hammer";
        public static string IconName = "chebgonaz_spiritpylon_icon.png";
        protected Stack<GameObject> spawnedGhosts = new Stack<GameObject>();

        public static RequirementConfig[] GetRequirements()
        {
            return new RequirementConfig[]
            {
                new RequirementConfig("Stone", 15, 0, true),
                new RequirementConfig("Wood", 15, 0, true),
                new RequirementConfig("BoneFragments", 15, 0, true),
            };
        }

        private void Awake()
        {
            Jotunn.Logger.LogInfo("SpiritPylon awakening...");
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

                if (Player.m_localPlayer != null)
                {
                    float playerNecromancyLevel = 
                        Player.m_localPlayer.GetSkillLevel(
                            SkillManager.Instance.GetSkill(BasePlugin.necromancySkillIdentifier)
                                .m_skill);
                    int amount = playerNecromancyLevel <= 19 
                        ? 1 
                        : (int)playerNecromancyLevel / 10;

                    if (EnemiesNearby())
                    {
                        // spawn ghosts up until the limit
                        if (spawnedGhosts.Count < amount)
                        {
                            spawnedGhosts.Push(SpawnFriendlyGhost(playerNecromancyLevel));
                        }
                    }  
                    else
                    {
                        // despawn ghosts one by one if there are no enemies
                        if (spawnedGhosts.Count > 0)
                        {
                            GameObject ghost = spawnedGhosts.Pop();
                            if (ghost != null)
                            {
                                if (ghost.TryGetComponent(out Character character))
                                {
                                    character.SetHealth(0);
                                }
                            }
                            else { Destroy(ghost); }
                        }
                    }
                }
            }
        }

        protected bool EnemiesNearby()
        {
            List<Character> charactersInRange = new List<Character>();
            Character.GetCharactersInRange(
                transform.position,
                sightRadius,
                charactersInRange
                );
            foreach (Character character in charactersInRange)
            {
                if (character != null && character.m_faction != Character.Faction.Players)
                {
                    return true;
                }
            }
            return false;
        }

        protected GameObject SpawnFriendlyGhost(float playerNecromancyLevel)
        {
            int quality = 1;
            if (playerNecromancyLevel >= 30) { quality = 3; }
            else if (playerNecromancyLevel >= 15) { quality = 2; }

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
