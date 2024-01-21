using System.Collections;
using BepInEx.Configuration;
using ChebsNecromancy.Items.Wands;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.Minions;
using Jotunn.Managers;
using UnityEngine;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

namespace ChebsNecromancy.Minions
{
    internal class BattleNeckroMinion : UndeadMinion
    {
        public const string PrefabName = "ChebGonaz_BattleNeckro";

        // for limits checking
        private static int _createdOrderIncrementer;

        public static ConfigEntry<bool> Allowed;

        public static ConfigEntry<int> MaxBattleNeckros;
        public static ConfigEntry<int> MinionLimitIncrementsEveryXLevels;

        public static ConfigEntry<float> BaseHP, BonusHPPerNecromancyLevel;
        
        public static ConfigEntry<float> NecromancyLevelIncrease;
        public static MemoryConfigEntry<string, List<string>> ItemsCost;
        
        public static ConfigEntry<int> TierOneQuality;
        public static ConfigEntry<int> TierTwoQuality;
        public static ConfigEntry<int> TierTwoLevelReq;
        public static ConfigEntry<int> TierThreeQuality;
        public static ConfigEntry<int> TierThreeLevelReq;

        public static void CreateConfigs(BasePlugin plugin)
        {
            const string serverSynced = "BattleNeckroMinion(Server Synced)";
            
            Allowed = plugin.Config.Bind(serverSynced, "Allowed",
                true, new ConfigDescription("Whether this minion can be made or not.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            MaxBattleNeckros = plugin.Config.Bind(serverSynced, "MaximumBattleNeckros",
                0, new ConfigDescription("The maximum Battle Neckro allowed to be created (0 = unlimited).", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            MinionLimitIncrementsEveryXLevels = plugin.Config.Bind(serverSynced,
                "MinionLimitIncrementsEveryXLevels",
                10, new ConfigDescription(
                    "Attention: has no effect if minion limits are off. Increases player's maximum minion count by 1 every X levels. For example, if the limit is 3 draugr and this is set to 10, then at level 10 Necromancy the player can have 4 minions. Then 5 at level 20, and so on.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            BaseHP = plugin.Config.Bind(serverSynced, "BaseHP",
                800f, new ConfigDescription("How much HP a Battle Neckro has before level scaling.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            BonusHPPerNecromancyLevel = plugin.Config.Bind(serverSynced, "BonusHPPerNecromancyLevel",
                2.5f, new ConfigDescription("How much extra HP a Battle Neckro gets per Necromancy level.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            TierOneQuality = plugin.Config.Bind(serverSynced, "TierOneQuality",
                1, new ConfigDescription("Star Quality of tier 1 minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            TierTwoQuality = plugin.Config.Bind(serverSynced, "TierTwoQuality",
                2, new ConfigDescription("Star Quality of tier 2  minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            TierTwoLevelReq = plugin.Config.Bind(serverSynced, "TierTwoLevelReq",
                50, new ConfigDescription("Necromancy skill level required to summon Tier 2 ", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            TierThreeQuality = plugin.Config.Bind(serverSynced, "TierThreeQuality",
                3, new ConfigDescription("Star Quality of tier 3 minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            TierThreeLevelReq = plugin.Config.Bind(serverSynced, "TierThreeLevelReq",
                75, new ConfigDescription("Necromancy skill level required to summon Tier 3", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            var itemsCost = plugin.ModConfig(serverSynced, "ItemsCost", 
                "RawMeat|DeerMeat|WolfMeat|LoxMeat|SerpentMeat|HareMeat|BugMeat|ChickenMeat:50",
                "The items that are consumed when creating a minion. Please use a comma-delimited list of prefab names with a : and integer for amount. Alternative items can be specified with a | eg. Wood|Coal:5 to mean wood and/or coal.",
                null, true);
            ItemsCost = new MemoryConfigEntry<string, List<string>>(itemsCost, s => s?.Split(',').Select(str => str.Trim()).ToList());
            
            NecromancyLevelIncrease = plugin.Config.Bind(serverSynced, "NecromancyLevelIncrease",
                5f, new ConfigDescription("The Necromancy level increase factor when creating this minion.", null,
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
        
        public static void InstantiateBattleNeckro(int quality, float playerNecromancyLevel)
        {
            var player = Player.m_localPlayer;
            var prefabName = PrefabName;
            var prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"{prefabName} does not exist");
                Logger.LogError($"InstantiateBattleNeckro: spawning {prefabName} failed");
            }

            var spawnedChar = Object.Instantiate(prefab,
                player.transform.position + player.transform.forward * 2f + Vector3.up, Quaternion.identity);
            spawnedChar.AddComponent<FreshMinion>();
            var minion = spawnedChar.AddComponent<BattleNeckroMinion>();
            minion.SetCreatedAtLevel(playerNecromancyLevel);
            var character = spawnedChar.GetComponent<Character>();
            character.SetLevel(quality);
            minion.ScaleStats(playerNecromancyLevel);

            player.RaiseSkill(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill,
                NecromancyLevelIncrease.Value);

            if (Wand.FollowByDefault.Value)
            {
                minion.Follow(player.gameObject);
            }
            else
            {
                minion.Wait(player.transform.position);
            }

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
        
        public static void ConsumeResources()
        {
            var inventory = Player.m_localPlayer.GetInventory();
            ConsumeRequirements(ItemsCost, inventory);
        }
    }
}