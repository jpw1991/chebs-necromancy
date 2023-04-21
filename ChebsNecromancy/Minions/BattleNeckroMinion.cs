using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using ChebsNecromancy.Items;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.Minions;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Minions
{
    public class BattleNeckroMinion : UndeadMinion
    {
        public const string PrefabName = "ChebGonaz_BattleNeckro";

        // for limits checking
        private static int _createdOrderIncrementer;

        public static ConfigEntry<bool> Allowed;
        
        public static ConfigEntry<DropType> DropOnDeath;
        public static ConfigEntry<bool> PackDropItemsIntoCargoCrate;

        public static ConfigEntry<int> MaxBattleNeckros;
        public static ConfigEntry<int> MinionLimitIncrementsEveryXLevels;
        public static ConfigEntry<int> MeatRequired;

        public static ConfigEntry<float> BaseHP, BonusHPPerNecromancyLevel;
        
        public static ConfigEntry<int> TierOneQuality;
        public static ConfigEntry<int> TierTwoQuality;
        public static ConfigEntry<int> TierTwoLevelReq;
        public static ConfigEntry<int> TierThreeQuality;
        public static ConfigEntry<int> TierThreeLevelReq;

        public new static void CreateConfigs(BaseUnityPlugin plugin)
        {
            Allowed = plugin.Config.Bind("BattleNeckroMinion (Server Synced)", "Allowed",
                true, new ConfigDescription("Whether this minion can be made or not.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            DropOnDeath = plugin.Config.Bind("BattleNeckroMinion (Server Synced)", "DropOnDeath",
                DropType.JustResources, new ConfigDescription("Whether a minion refunds anything when it dies.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            PackDropItemsIntoCargoCrate = plugin.Config.Bind("BattleNeckroMinion (Server Synced)",
                "PackDroppedItemsIntoCargoCrate",
                true, new ConfigDescription(
                    "If set to true, dropped items will be packed into a cargo crate. This means they won't sink in water, which is useful for more valuable drops like Surtling Cores and metal ingots.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            MaxBattleNeckros = plugin.Config.Bind("BattleNeckroMinion (Server Synced)", "MaximumBattleNeckros",
                0, new ConfigDescription("The maximum Battle Neckro allowed to be created (0 = unlimited).", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            MinionLimitIncrementsEveryXLevels = plugin.Config.Bind("BattleNeckroMinion (Server Synced)",
                "MinionLimitIncrementsEveryXLevels",
                10, new ConfigDescription(
                    "Attention: has no effect if minion limits are off. Increases player's maximum minion count by 1 every X levels. For example, if the limit is 3 draugr and this is set to 10, then at level 10 Necromancy the player can have 4 minions. Then 5 at level 20, and so on.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            BaseHP = plugin.Config.Bind("BattleNeckroMinion (Server Synced)", "BaseHP",
                800f, new ConfigDescription("How much HP a Battle Neckro has before level scaling.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            BonusHPPerNecromancyLevel = plugin.Config.Bind("BattleNeckroMinion (Server Synced)", "BonusHPPerNecromancyLevel",
                2.5f, new ConfigDescription("How much extra HP a Battle Neckro gets per Necromancy level.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            MeatRequired = plugin.Config.Bind("BattleNeckroMinion (Server Synced)", "MeatRequired",
                50, new ConfigDescription("How much meat is required to make a Battle Neckro.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            TierOneQuality = plugin.Config.Bind($"BattleNeckroMinion (Server Synced)", "TierOneQuality",
                1, new ConfigDescription("Star Quality of tier 1 minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            TierTwoQuality = plugin.Config.Bind($"BattleNeckroMinion (Server Synced)", "TierTwoQuality",
                2, new ConfigDescription("Star Quality of tier 2  minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            TierTwoLevelReq = plugin.Config.Bind($"BattleNeckroMinion (Server Synced)", "TierTwoLevelReq",
                50, new ConfigDescription("Necromancy skill level required to summon Tier 2 ", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            TierThreeQuality = plugin.Config.Bind($"BattleNeckroMinion (Server Synced)", "TierThreeQuality",
                3, new ConfigDescription("Star Quality of tier 3 minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            TierThreeLevelReq = plugin.Config.Bind($"BattleNeckroMinion (Server Synced)", "TierThreeLevelReq",
                75, new ConfigDescription("Necromancy skill level required to summon Tier 3", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        public override void Awake()
        {
            base.Awake();

            _createdOrderIncrementer++;
            createdOrder = _createdOrderIncrementer;

            StartCoroutine(WaitForZNet());
        }

        IEnumerator WaitForZNet()
        {
            yield return new WaitUntil(() => ZNetScene.instance != null);

            ScaleStats(GetCreatedAtLevel());

            // by the time player arrives, ZNet stuff is certainly ready
            if (!TryGetComponent(out Humanoid humanoid))
            {
                Logger.LogError("Humanoid component missing!");
                yield break;
            }

            RestoreDrops();

            // wondering what the code below does? Check comments in the
            // FreshMinion.cs file.
            var freshMinion = GetComponent<FreshMinion>();
            var monsterAI = GetComponent<MonsterAI>();
            monsterAI.m_randomMoveRange = RoamRange.Value;
            if (!Wand.FollowByDefault.Value || freshMinion == null)
            {
                yield return new WaitUntil(() => Player.m_localPlayer != null);

                RoamFollowOrWait();
            }

            if (freshMinion != null)
            {
                // remove the component
                Destroy(freshMinion);
            }
        }

        public void ScaleStats(float necromancyLevel)
        {
            var character = GetComponent<Character>();
            if (character == null)
            {
                Logger.LogError("ScaleStats: Character component is null!");
                return;
            }

            var health = BaseHP.Value + necromancyLevel * BonusHPPerNecromancyLevel.Value;
            character.SetMaxHealth(health);
            character.SetHealth(health);
        }
    }
}