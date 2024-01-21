using System.Collections;
using BepInEx.Configuration;
using ChebsNecromancy.Items.Wands;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.Minions;
using Jotunn.Managers;
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
        
        public static ConfigEntry<bool> Allowed;

        public static ConfigEntry<int> MaxLeeches;
        public static ConfigEntry<int> MinionLimitIncrementsEveryXLevels;

        public static ConfigEntry<float> LeechBaseHealth;
        public static ConfigEntry<float> LeechHealthMultiplier;

        public static ConfigEntry<int> LeechTierOneQuality;
        public static ConfigEntry<int> LeechTierTwoQuality;
        public static ConfigEntry<int> LeechTierTwoLevelReq;
        public static ConfigEntry<int> LeechTierThreeQuality;
        public static ConfigEntry<int> LeechTierThreeLevelReq;

        public static ConfigEntry<float> LevelIncrease;

        public static MemoryConfigEntry<string, List<string>> ItemsCost;

        public static void CreateConfigs(BasePlugin plugin)
        {
            const string serverSynced = "LeechMinion (Server Synced)";
            Allowed = plugin.Config.Bind(serverSynced,
                "Allowed",
                true, new ConfigDescription(
                    "Set to false to disable leeches.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            MaxLeeches = plugin.Config.Bind(serverSynced, "MaximumLeech",
                0, new ConfigDescription("The maximum leeches allowed to be created (0 = unlimited).", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            MinionLimitIncrementsEveryXLevels = plugin.Config.Bind(serverSynced,
                "MinionLimitIncrementsEveryXLevels",
                10, new ConfigDescription(
                    "Attention: has no effect if minion limits are off. Increases player's maximum minion count by 1 every X levels. For example, if the limit is 3 leeches and this is set to 10, then at level 10 Necromancy the player can have 4 minions. Then 5 at level 20, and so on.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            LeechBaseHealth = plugin.Config.Bind(serverSynced, "LeechBaseHealth",
                50f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            LeechHealthMultiplier = plugin.Config.Bind(serverSynced, "LeechHealthMultiplier",
                .5f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            var itemsCost = plugin.ModConfig(serverSynced, "ItemsCost", "Entrails|Bloodbag:1",
                "The items that are consumed when creating a Leech. Please use a comma-delimited list of prefab names with a : and integer for amount.",
                null, true);
            ItemsCost = new MemoryConfigEntry<string, List<string>>(itemsCost, s => s?.Split(',').Select(str => str.Trim()).ToList());

            LeechTierOneQuality = plugin.Config.Bind(serverSynced, "LeechTierOneQuality",
                1, new ConfigDescription("Star Quality of tier 1 Leech minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            LeechTierTwoQuality = plugin.Config.Bind(serverSynced, "LeechTierTwoQuality",
                2, new ConfigDescription("Star Quality of tier 2 Leech minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            LeechTierTwoLevelReq = plugin.Config.Bind(serverSynced, "LeechTierTwoLevelReq",
                35, new ConfigDescription("Necromancy skill level required to summon Tier 2 Leech", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            LeechTierThreeQuality = plugin.Config.Bind(serverSynced, "LeechTierThreeQuality",
                3, new ConfigDescription("Star Quality of tier 3 Leech minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            LeechTierThreeLevelReq = plugin.Config.Bind(serverSynced, "LeechTierThreeLevelReq",
                70, new ConfigDescription("Necromancy skill level required to summon Tier 3 Leech", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            LevelIncrease = plugin.Config.Bind(serverSynced, "LevelIncrease",
                1f, new ConfigDescription("How much creating a leech contributes to necromancy level.", null,
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

        public static void ConsumeResources(LeechType leechType)
        {
            if (leechType is LeechType.None) return;

            ConsumeRequirements(ItemsCost, Player.m_localPlayer.GetInventory());
        }

        public static void InstantiateLeech(int quality, float playerNecromancyLevel, LeechType leechType)
        {
            if (leechType is LeechType.None) return;

            var player = Player.m_localPlayer;
            var prefabName = InternalName.GetName(leechType);
            var prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"{prefabName} does not exist");
                Logger.LogError($"InstantiateLeech: spawning {prefabName} failed");
                return;
            }

            var transform = player.transform;
            var spawnedChar = Instantiate(prefab,
                transform.position + transform.forward * 2f + Vector3.up, Quaternion.identity);
            var character = spawnedChar.GetComponent<Character>();
            character.SetLevel(quality);

            spawnedChar.AddComponent<FreshMinion>();

            var minion = spawnedChar.AddComponent<LeechMinion>();
            minion.SetCreatedAtLevel(playerNecromancyLevel);
            minion.ScaleStats(playerNecromancyLevel);

            if (Wand.FollowByDefault.Value)
            {
                minion.Follow(player.gameObject);
            }
            else
            {
                minion.Wait(player.transform.position);
            }

            player.RaiseSkill(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill,
                LevelIncrease.Value);

            minion.UndeadMinionMaster = player.GetPlayerName();

            // handle refunding of resources on death
            if (DropOnDeath.Value != DropType.Everything) return;

            var characterDrop = spawnedChar.AddComponent<CharacterDrop>();
            GenerateDeathDrops(characterDrop, ItemsCost);

            // the component won't be remembered by the game on logout because
            // only what is on the prefab is remembered. Even changes to the prefab
            // aren't remembered. So we must write what we're dropping into
            // the ZDO as well and then read & restore this on Awake
            minion.RecordDrops(characterDrop);
        }
    }
}