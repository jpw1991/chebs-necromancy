using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using ChebsNecromancy.Items;
using ChebsNecromancy.Minions.AI;
using UnityEngine;

namespace ChebsNecromancy.Minions
{
    internal class SkeletonWoodcutterMinion : UndeadMinion
    {
        public static ConfigEntry<int> ToolTier;

        public new static void CreateConfigs(BaseUnityPlugin plugin)
        {
            ToolTier = plugin.Config.Bind("SkeletonWoodcutter (Server Synced)", "ToolTier",
                3, new ConfigDescription("The tier of the skeleton's tool: 0 (stone), 1 (flint), 2 (bronze), 3 (iron) or 4 (black metal).", null,
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