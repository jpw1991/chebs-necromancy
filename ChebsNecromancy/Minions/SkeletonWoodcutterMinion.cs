using BepInEx;
using BepInEx.Configuration;
using ChebsNecromancy.Minions.AI;

namespace ChebsNecromancy.Minions
{
    internal class SkeletonWoodcutterMinion : SkeletonMinion
    {
        public static ConfigEntry<int> ToolTier;
        public static ConfigEntry<float> UpdateDelay, LookRadius, RoamRange;

        public new static void CreateConfigs(BaseUnityPlugin plugin)
        {
            ToolTier = plugin.Config.Bind("SkeletonWoodcutter (Server Synced)", "ToolTier",
                3, new ConfigDescription("The tier of the skeleton's tool: 0 (stone), 1 (flint), 2 (bronze), 3 (iron) or 4 (black metal).", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            UpdateDelay = plugin.Config.Bind("SkeletonWoodcutter (Server Synced)", "SkeletonWoodcutterUpdateDelay",
                6f, new ConfigDescription("The delay, in seconds, between wood searching attempts. Attention: small values may impact performance.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            LookRadius = plugin.Config.Bind("SkeletonWoodcutter (Server Synced)", "LookRadius",
                50f, new ConfigDescription("How far it can see wood. High values may damage performance.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            RoamRange = plugin.Config.Bind("SkeletonWoodcutter (Server Synced)", "RoamRange",
                50f, new ConfigDescription("How far it will randomly run to in search of wood.", null,
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