using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using ChebsNecromancy.Items;
using ChebsNecromancy.Minions.AI;
using UnityEngine;

namespace ChebsNecromancy.Minions
{
    internal class SkeletonWoodcutterMinion : SkeletonMinion
    {
        public static ConfigEntry<int> ToolTier;
        public static ConfigEntry<float> UpdateDelay;

        public new static void CreateConfigs(BaseUnityPlugin plugin)
        {
            ToolTier = plugin.Config.Bind("SkeletonWoodcutter (Server Synced)", "ToolTier",
                3, new ConfigDescription("The tier of the skeleton's tool: 0 (stone), 1 (flint), 2 (bronze), 3 (iron) or 4 (black metal).", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            UpdateDelay = plugin.Config.Bind("SkeletonWoodcutter (Server Synced)", "SkeletonWoodcutterUpdateDelay",
                6f, new ConfigDescription("The delay, in seconds, between wood searching attempts. Attention: small values may impact performance.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        public override void Awake()
        {
            base.Awake();

            canBeCommanded = false;

            if (!TryGetComponent(out WoodcutterAI _)) gameObject.AddComponent<WoodcutterAI>();
        }
    }
}