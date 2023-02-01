using BepInEx;
using BepInEx.Configuration;
using Jotunn;
using Jotunn.Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChebsNecromancy
{
    internal class GuardianWraithMinion : UndeadMinion
    {
        public static ConfigEntry<int> guardianWraithLevelRequirement;
        public static ConfigEntry<int> guardianWraithDuration;

        private float killAt;

        public static new void CreateConfigs(BaseUnityPlugin plugin)
        {
            guardianWraithLevelRequirement = plugin.Config.Bind("SpectralShroud (Server Synced)", "GuardianWraithLevelRequirement",
                25, new ConfigDescription("The Necromancy level required to control a Guardian Wraith.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            guardianWraithDuration = plugin.Config.Bind("SpectralShroud (Server Synced)", "GuardianWraithDuration",
                10, new ConfigDescription("The lifetime of a Guardian Wraith.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        public override void Awake()
        {
            base.Awake();

            killAt = Time.time + guardianWraithDuration.Value;
            canBeCommanded = false;
        }

        private void Update()
        {
            if (Time.time > killAt)
            {
                Kill();
            }
            else if (Player.m_localPlayer != null
                && Player.m_localPlayer.IsTeleporting()
                && BelongsToPlayer(Player.m_localPlayer.GetPlayerName()))
            {
                Kill();
            }
        }
    }
}
