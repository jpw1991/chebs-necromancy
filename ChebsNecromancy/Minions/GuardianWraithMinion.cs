using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace ChebsNecromancy.Minions
{
    internal class GuardianWraithMinion : UndeadMinion
    {
        public static ConfigEntry<int> GuardianWraithLevelRequirement;
        public static ConfigEntry<int> GuardianWraithDuration;

        private float killAt;

        public new static void CreateConfigs(BaseUnityPlugin plugin)
        {
            GuardianWraithLevelRequirement = plugin.Config.Bind("SpectralShroud (Server Synced)", "GuardianWraithLevelRequirement",
                25, new ConfigDescription("The Necromancy level required to control a Guardian Wraith.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            GuardianWraithDuration = plugin.Config.Bind("SpectralShroud (Server Synced)", "GuardianWraithDuration",
                10, new ConfigDescription("The lifetime of a Guardian Wraith.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        public override void Awake()
        {
            base.Awake();

            killAt = Time.time + GuardianWraithDuration.Value;
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
