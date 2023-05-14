using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.Minions.AI;

namespace ChebsNecromancy.Minions.Skeletons
{
    internal class SkeletonWoodcutterMinion : SkeletonMinion
    {
        public static ConfigEntry<float> UpdateDelay, LookRadius;
        public static MemoryConfigEntry<string, List<string>> ItemsCost;

        public new static void CreateConfigs(BasePlugin plugin)
        {
            UpdateDelay = plugin.Config.Bind("SkeletonWoodcutter (Server Synced)", "SkeletonWoodcutterUpdateDelay",
                6f, new ConfigDescription("The delay, in seconds, between wood searching attempts. Attention: small values may impact performance.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            LookRadius = plugin.Config.Bind("SkeletonWoodcutter (Server Synced)", "LookRadius",
                50f, new ConfigDescription("How far it can see wood. High values may damage performance.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            var itemsCost = plugin.ModConfig("SkeletonWoodcutter (Server Synced)", "ItemsCost", "BoneFragments:3,Flint:1",
                "The items that are consumed when creating a minion. Please use a comma-delimited list of prefab names with a : and integer for amount.",
                null, true);
            ItemsCost = new MemoryConfigEntry<string, List<string>>(itemsCost, s => s?.Split(',').ToList());
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