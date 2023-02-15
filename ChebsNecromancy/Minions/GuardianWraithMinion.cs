using BepInEx.Configuration;
using UnityEngine;

namespace ChebsNecromancy.Minions
{
    internal class GuardianWraithMinion : UndeadMinion
    {
        public static ConfigEntry<int> GuardianWraithLevelRequirement;
        public static ConfigEntry<int> GuardianWraithDuration;

        private float killAt;

        public new static void CreateConfigs(BasePlugin plugin)
        {
            GuardianWraithLevelRequirement = plugin.ModConfig("SpectralShroud", "GuardianWraithLevelRequirement",
                25, "The Necromancy level required to control a Guardian Wraith.", plugin.IntQuantityValue, true);
            GuardianWraithDuration = plugin.ModConfig("SpectralShroud", "GuardianWraithDuration",
                10, "The lifetime of a Guardian Wraith.", plugin.IntQuantityValue, true);
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
