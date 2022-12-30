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
        public static ConfigEntry<int> guardianWraithDuration;

        private float createdAt;

        public static void CreateConfigs(BaseUnityPlugin plugin)
        {
            guardianWraithLevelRequirement = plugin.Config.Bind("Client config", "GuardianWraithLevelRequirement",
                25, new ConfigDescription("The Necromancy level required to control a Guardian Wraith."));
            guardianWraithDuration = plugin.Config.Bind("Client config", "GuardianWraithDuration",
                10, new ConfigDescription("The lifetime of a Guardian Wraith."));
        }

        private void Awake()
        {
            createdAt = Time.time;
            canBeCommanded = false;
        }

        private void Update()
        {
            if (Time.time > createdAt + guardianWraithDuration.Value)
            {
                if (TryGetComponent(out Humanoid humanoid))
                {
                    humanoid.SetHealth(0);
                }
            }
        }
    }
}
