using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using ChebsNecromancy.Items;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Minions
{
    internal class LeechMinion : UndeadMinion
    {
        public enum LeechType
        {
            None,
            [InternalName("ChebGonaz_Leech")] Leech,
        };

        // for limits checking
        private static int _createdOrderIncrementer;

        public static ConfigEntry<DropType> DropOnDeath;
        public static ConfigEntry<bool> PackDropItemsIntoCargoCrate;

        public static ConfigEntry<int> MaxLeeches;
        public static ConfigEntry<int> MinionLimitIncrementsEveryXLevels;
        
        public static ConfigEntry<float> LeechBaseHealth;
        public static ConfigEntry<float> LeechHealthMultiplier;

        public new static void CreateConfigs(BaseUnityPlugin plugin)
        {
            DropOnDeath = plugin.Config.Bind("LeechMinion (Server Synced)", "DropOnDeath",
                DropType.JustResources, new ConfigDescription("Whether a minion refunds anything when it dies.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            PackDropItemsIntoCargoCrate = plugin.Config.Bind("LeechMinion (Server Synced)",
                "PackDroppedItemsIntoCargoCrate",
                true, new ConfigDescription(
                    "If set to true, dropped items will be packed into a cargo crate. This means they won't sink in water, which is useful for more valuable drops like Surtling Cores and metal ingots.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            MaxLeeches = plugin.Config.Bind("LeechMinion (Server Synced)", "MaximumLeech",
                0, new ConfigDescription("The maximum leeches allowed to be created (0 = unlimited).", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            MinionLimitIncrementsEveryXLevels = plugin.Config.Bind("LeechMinion (Server Synced)",
                "MinionLimitIncrementsEveryXLevels",
                10, new ConfigDescription(
                    "Attention: has no effect if minion limits are off. Increases player's maximum minion count by 1 every X levels. For example, if the limit is 3 leeches and this is set to 10, then at level 10 Necromancy the player can have 4 minions. Then 5 at level 20, and so on.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            LeechBaseHealth = plugin.Config.Bind("LeechMinion (Server Synced)", "LeechBaseHealth",
                50f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            LeechHealthMultiplier = plugin.Config.Bind("LeechMinion (Server Synced)", "LeechHealthMultiplier",
                .5f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier", null,
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
            FreshMinion freshMinion = GetComponent<FreshMinion>();
            MonsterAI monsterAI = GetComponent<MonsterAI>();
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
            Character character = GetComponent<Character>();
            if (character == null)
            {
                Logger.LogError("ScaleStats: Character component is null!");
                return;
            }

            float health = LeechBaseHealth.Value +
                           necromancyLevel * LeechHealthMultiplier.Value;
            character.SetMaxHealth(health);
            character.SetHealth(health);
        }
    }
}