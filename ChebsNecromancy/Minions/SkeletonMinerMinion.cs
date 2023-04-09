using BepInEx;
using BepInEx.Configuration;
using ChebsValheimLibrary.Minions.AI;

namespace ChebsNecromancy.Minions
{
    internal class SkeletonMinerMinion : SkeletonMinion
    {
        public static ConfigEntry<int> ToolTier;
        public static ConfigEntry<float> UpdateDelay, LookRadius, RoamRange;
        public static ConfigEntry<string> RockInternalIDsList;

        private const string DefaultOresList = "rock1_mistlands,rock1_mountain,rock1_mountain_frac,rock2_heath,rock2_heath_frac,rock2_mountain,rock2_mountain_frac,Rock_3,Rock_3_frac,rock3_mountain,rock3_mountain_frac,rock3_silver,rock3_silver_frac,Rock_4,Rock_4_plains,rock4_coast,rock4_coast_frac,rock4_copper,rock4_copper_frac,rock4_forest,rock4_forest_frac,rock4_heath,rock4_heath_frac,Rock_7,Rock_destructible,rock_mistlands1,rock_mistlands1_frac,rock_mistlands2,RockDolmen_1,RockDolmen_2,RockDolmen_3,silvervein,silvervein_frac,MineRock_Tin,MineRock_Obsidian";

        public new static void CreateConfigs(BaseUnityPlugin plugin)
        {
            ToolTier = plugin.Config.Bind("SkeletonMiner (Server Synced)", "ToolTier",
                3, new ConfigDescription("The tier of the skeleton's tool: 0 (deerbone), 1 (bronze), 2 (iron), 3 (black metal).", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            UpdateDelay = plugin.Config.Bind("SkeletonMiner (Server Synced)", "SkeletonMinerUpdateDelay",
                6f, new ConfigDescription("The delay, in seconds, between rock/ore searching attempts. Attention: small values may impact performance.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            LookRadius = plugin.Config.Bind("SkeletonMiner (Server Synced)", "LookRadius",
                50f, new ConfigDescription("How far it can see rock/ore. High values may damage performance.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            RoamRange = plugin.Config.Bind("SkeletonMiner (Server Synced)", "RoamRange",
                50f, new ConfigDescription("How far it will randomly run to in search of rock/ore.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            RockInternalIDsList = plugin.Config.Bind("SkeletonMiner (Server Synced)", "RockInternalIDsList",
                DefaultOresList, new ConfigDescription("The types of rock the miner will attempt to mine. Internal IDs only.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        public override void Awake()
        {
            base.Awake();

            canBeCommanded = false;

            if (!TryGetComponent(out MinerAI _)) gameObject.AddComponent<MinerAI>();
        }
        
        public static void SyncInternalsWithConfigs()
        {
            // awful stuff. Is there a better way?
            MinerAI.UpdateDelay = UpdateDelay.Value;
            MinerAI.LookRadius = LookRadius.Value;
            MinerAI.RoamRange = RoamRange.Value;
            MinerAI.RockInternalIDsList = RockInternalIDsList.Value;
        }
    }
}