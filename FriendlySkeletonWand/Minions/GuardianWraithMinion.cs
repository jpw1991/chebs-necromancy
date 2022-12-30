using BepInEx;
using BepInEx.Configuration;
using Jotunn;
using Jotunn.Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FriendlySkeletonWand
{
    internal class GuardianWraithMinion : UndeadMinion
    {
        public static ConfigEntry<int> guardianWraithLevelRequirement;
        public static ConfigEntry<float> guardianWraithTetherDistance;

        private float updateDelay;

        public static void CreateConfigs(BaseUnityPlugin plugin)
        {
            guardianWraithLevelRequirement = plugin.Config.Bind("Client config", "GuardianWraithLevelRequirement",
                25, new ConfigDescription("The Necromancy level required to control a Guardian Wraith."));
            guardianWraithTetherDistance = plugin.Config.Bind("Client config", "GuardianWraithTetherDistance",
                30f, new ConfigDescription("How far a Guardian Wraith can be from the player before it is teleported back to you."));
        }

        public static GameObject instance;

        private void Awake()
        {
            canBeCommanded = false;
        }

        private void Update()
        {
            if (SpectralShroud.spawnWraith.Value
                && ZInput.instance != null
                && Player.m_localPlayer != null)
            {
                if (Time.time > updateDelay)
                {
                    //if (instance != null && instance != gameObject)
                    //{
                    //    GetComponent<Humanoid>().SetHealth(0);
                    //}

                    TetherToPlayer();

                    updateDelay = Time.time + 5f;
                }
            }
        }

        private void TetherToPlayer()
        {
            Player player = Player.m_localPlayer;
            if (player != null)
            {
                MonsterAI monsterAI = GetComponent<MonsterAI>();
                if (monsterAI != null)
                {
                    monsterAI.SetFollowTarget(player.gameObject);
                    if (Vector3.Distance(player.transform.position, transform.position) > guardianWraithTetherDistance.Value)
                    {
                        transform.position = player.transform.position;
                        // todo: make it forget its current target
                    }
                }
            }
        }
    }
}
