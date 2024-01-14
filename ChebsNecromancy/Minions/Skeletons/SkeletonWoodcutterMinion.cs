using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using ChebsNecromancy.Minions.Skeletons.WorkerAI;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.Minions.AI;

namespace ChebsNecromancy.Minions.Skeletons
{
    internal class SkeletonWoodcutterMinion : SkeletonMinion
    {
        public static ConfigEntry<float> UpdateDelay, LookRadius, ToolDamage, ChatInterval, ChatDistance;
        public static ConfigEntry<short> ToolTier;
        public static MemoryConfigEntry<string, List<string>> ItemsCost;

        public static void CreateConfigs(BasePlugin plugin)
        {
            const string serverSyncedHeading = "SkeletonWoodcutter (Server Synced)";
            UpdateDelay = plugin.Config.Bind(serverSyncedHeading, "SkeletonWoodcutterUpdateDelay",
                6f, new ConfigDescription("The delay, in seconds, between wood searching attempts. Attention: small values may impact performance.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            LookRadius = plugin.Config.Bind(serverSyncedHeading, "LookRadius",
                50f, new ConfigDescription("How far it can see wood. High values may damage performance.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            var itemsCost = plugin.ModConfig(serverSyncedHeading, "ItemsCost", "BoneFragments:6,Flint:1",
                "The items that are consumed when creating a minion. Please use a comma-delimited list of prefab names with a : and integer for amount. Alternative items can be specified with a | eg. Wood|Coal:5 to mean wood and/or coal.",
                null, true);
            ItemsCost = new MemoryConfigEntry<string, List<string>>(itemsCost, s => s?.Split(',').Select(str => str.Trim()).ToList());
            ToolDamage = plugin.Config.Bind(serverSyncedHeading, "ToolDamage", 6f,
                new ConfigDescription("Damage dealt by the worker's tool to stuff it's harvesting.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            ToolTier = plugin.Config.Bind(serverSyncedHeading, "ToolTier", (short)2,
                new ConfigDescription("Worker's tool tier (determines what stuff it can mine/harvest).", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            ChatInterval = plugin.Config.Bind(serverSyncedHeading, "ChatInterval", 6f,
                new ConfigDescription("The delay, in seconds, between worker updates. Set to 0 for no chatting.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            ChatDistance = plugin.Config.Bind(serverSyncedHeading, "ChatDistance", 6f,
                new ConfigDescription("How close a player must be for the worker to speak.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        public override void Awake()
        {
            base.Awake();

            canBeCommanded = false;

            if (!TryGetComponent(out SkeletonWoodcutterAI _)) gameObject.AddComponent<SkeletonWoodcutterAI>();
        }
    }
}