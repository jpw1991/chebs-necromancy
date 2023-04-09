using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using ChebsNecromancy.Items;
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

        public static ConfigEntry<DropType> DropOnDeath;
        public static ConfigEntry<bool> PackDropItemsIntoCargoCrate, Allowed;

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

        public static ConfigEntry<int> BloodBagsRequired, IntestinesRequired;

        public new static void CreateConfigs(BaseUnityPlugin plugin)
        {
            Allowed = plugin.Config.Bind("LeechMinion (Server Synced)",
                "Allowed",
                true, new ConfigDescription(
                    "Set to false to disable leeches.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
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
            
            BloodBagsRequired = plugin.Config.Bind("LeechMinion (Server Synced)",
                "BloodBagsRequired",
                2, new ConfigDescription(
                    "The amount of bags required to summon a leech.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            IntestinesRequired = plugin.Config.Bind("LeechMinion (Server Synced)",
                "IntestinesRequired",
                2, new ConfigDescription(
                    "The amount of intestines required to summon a leech.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            LeechTierOneQuality = plugin.Config.Bind("LeechMinion (Server Synced)", "LeechTierOneQuality",
                1, new ConfigDescription("Star Quality of tier 1 Leech minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            LeechTierTwoQuality = plugin.Config.Bind("LeechMinion (Server Synced)", "LeechTierTwoQuality",
                2, new ConfigDescription("Star Quality of tier 2 Leech minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            LeechTierTwoLevelReq = plugin.Config.Bind("LeechMinion (Server Synced)", "LeechTierTwoLevelReq",
                35, new ConfigDescription("Necromancy skill level required to summon Tier 2 Leech", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            LeechTierThreeQuality = plugin.Config.Bind("LeechMinion (Server Synced)", "LeechTierThreeQuality",
                3, new ConfigDescription("Star Quality of tier 3 Leech minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            LeechTierThreeLevelReq = plugin.Config.Bind("LeechMinion (Server Synced)", "LeechTierThreeLevelReq",
                70, new ConfigDescription("Necromancy skill level required to summon Tier 3 Leech", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            LevelIncrease = plugin.Config.Bind("LeechMinion (Server Synced)", "LevelIncrease",
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
            
            var player = Player.m_localPlayer;
            
            if (BloodBagsRequired.Value > 0)
                player.GetInventory().RemoveItem("$item_bloodbag", BloodBagsRequired.Value);
            if (IntestinesRequired.Value > 0)
                player.GetInventory().RemoveItem("$item_entrails", IntestinesRequired.Value);
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
            if (DropOnDeath.Value != DropType.Nothing)
            {
                var characterDrop = minion.gameObject.AddComponent<CharacterDrop>();

                if (DropOnDeath.Value == DropType.Everything
                    && BloodBagsRequired.Value > 0)
                {
                    if (BloodBagsRequired.Value > 0)
                    {
                        characterDrop.m_drops.Add(new CharacterDrop.Drop
                        {
                            m_prefab = ZNetScene.instance.GetPrefab("Bloodbag"),
                            m_onePerPlayer = true,
                            m_amountMin = BloodBagsRequired.Value,
                            m_amountMax = BloodBagsRequired.Value,
                            m_chance = 1f
                        });   
                    }

                    if (IntestinesRequired.Value > 0)
                    {
                        characterDrop.m_drops.Add(new CharacterDrop.Drop
                        {
                            m_prefab = ZNetScene.instance.GetPrefab("Entrails"),
                            m_onePerPlayer = true,
                            m_amountMin = IntestinesRequired.Value,
                            m_amountMax = IntestinesRequired.Value,
                            m_chance = 1f
                        });
                    }
                }

                // the component won't be remembered by the game on logout because
                // only what is on the prefab is remembered. Even changes to the prefab
                // aren't remembered. So we must write what we're dropping into
                // the ZDO as well and then read & restore this on Awake
                minion.RecordDrops(characterDrop);
            }
        }
    }
}