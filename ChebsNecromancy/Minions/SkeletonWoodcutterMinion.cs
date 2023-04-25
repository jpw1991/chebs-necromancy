using BepInEx;
using BepInEx.Configuration;
using ChebsValheimLibrary.Minions.AI;

namespace ChebsNecromancy.Minions
{
    internal class SkeletonWoodcutterMinion : SkeletonMinion
    {
        public static ConfigEntry<float> UpdateDelay, LookRadius;

        public new static void CreateConfigs(BaseUnityPlugin plugin)
        {
            UpdateDelay = plugin.Config.Bind("SkeletonWoodcutter (Server Synced)", "SkeletonWoodcutterUpdateDelay",
                6f, new ConfigDescription("The delay, in seconds, between wood searching attempts. Attention: small values may impact performance.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            LookRadius = plugin.Config.Bind("SkeletonWoodcutter (Server Synced)", "LookRadius",
                50f, new ConfigDescription("How far it can see wood. High values may damage performance.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        public override void Awake()
        {
            base.Awake();

            canBeCommanded = false;

            if (!TryGetComponent(out WoodcutterAI _)) gameObject.AddComponent<WoodcutterAI>();
        }
        
        public static void SyncInternalsWithConfigs()
        {
            // awful stuff. Is there a better way?
            WoodcutterAI.UpdateDelay = UpdateDelay.Value;
            WoodcutterAI.LookRadius = LookRadius.Value;
            WoodcutterAI.RoamRange = RoamRange.Value;
        }
    }
}