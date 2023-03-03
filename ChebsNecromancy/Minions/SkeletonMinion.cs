using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using ChebsNecromancy.Items;
using Jotunn.Managers;
using UnityEngine;
using Logger = Jotunn.Logger;
using Random = UnityEngine.Random;

namespace ChebsNecromancy.Minions
{
    internal class SkeletonMinion : UndeadMinion
    {
        public enum SkeletonType
        {
            None,
            [InternalName("ChebGonaz_SkeletonWarrior")] WarriorTier1,
            [InternalName("ChebGonaz_SkeletonWarriorTier2")] WarriorTier2,
            [InternalName("ChebGonaz_SkeletonWarriorTier3")] WarriorTier3,
            [InternalName("ChebGonaz_SkeletonWarriorTier4")] WarriorTier4,
            [InternalName("ChebGonaz_SkeletonArcher")] ArcherTier1,
            [InternalName("ChebGonaz_SkeletonArcherTier2")] ArcherTier2,
            [InternalName("ChebGonaz_SkeletonArcherTier3")] ArcherTier3,
            [InternalName("ChebGonaz_SkeletonArcherPoison")] ArcherPoison,
            [InternalName("ChebGonaz_SkeletonArcherFire")] ArcherFire,
            [InternalName("ChebGonaz_SkeletonArcherFrost")] ArcherFrost,
            [InternalName("ChebGonaz_SkeletonArcherSilver")] ArcherSilver,
            [InternalName("ChebGonaz_SkeletonMage")] MageTier1,
            [InternalName("ChebGonaz_SkeletonMageTier2")] MageTier2,
            [InternalName("ChebGonaz_SkeletonMageTier3")] MageTier3,
            [InternalName("ChebGonaz_PoisonSkeleton")] PoisonTier1,
            [InternalName("ChebGonaz_PoisonSkeleton2")] PoisonTier2,
            [InternalName("ChebGonaz_PoisonSkeleton3")] PoisonTier3,
            [InternalName("ChebGonaz_SkeletonWoodcutter")] Woodcutter,
            [InternalName("ChebGonaz_SkeletonMiner")] Miner,
            [InternalName("ChebGonaz_SkeletonWarriorNeedle")] WarriorNeedle,
        };

        // for limits checking
        private static int _createdOrderIncrementer;
        public int createdOrder;

        public static ConfigEntry<DropType> DropOnDeath;
        public static ConfigEntry<bool> PackDropItemsIntoCargoCrate;
        
        public static ConfigEntry<float> NecromancyLevelIncrease;
        public static ConfigEntry<float> PoisonNecromancyLevelIncrease;
        public static ConfigEntry<float> ArcherNecromancyLevelIncrease;
        public static ConfigEntry<float> MageNecromancyLevelIncrease;
        
        public static ConfigEntry<int> MaxSkeletons;
        public static ConfigEntry<int> MinionLimitIncrementsEveryXLevels;

        public new static void CreateConfigs(BaseUnityPlugin plugin)
        {
            DropOnDeath = plugin.Config.Bind("SkeletonMinion (Server Synced)", 
                "DropOnDeath",
                DropType.JustResources, new ConfigDescription("Whether a minion refunds anything when it dies.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            PackDropItemsIntoCargoCrate = plugin.Config.Bind("SkeletonMinion (Server Synced)", 
                "PackDroppedItemsIntoCargoCrate",
                true, new ConfigDescription("If set to true, dropped items will be packed into a cargo crate. This means they won't sink in water, which is useful for more valuable drops like Surtling Cores and metal ingots.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            NecromancyLevelIncrease = plugin.Config.Bind("SkeletonMinion (Server Synced)", 
                "NecromancyLevelIncrease",
                .75f, new ConfigDescription(
                    "How much crafting a skeleton contributes to your Necromancy level increasing.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            PoisonNecromancyLevelIncrease = plugin.Config.Bind("SkeletonMinion (Server Synced)",
                "PoisonSkeletonNecromancyLevelIncrease",
                1f, new ConfigDescription(
                    "How much crafting a Poison Skeleton contributes to your Necromancy level increasing.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            ArcherNecromancyLevelIncrease = plugin.Config.Bind("SkeletonMinion (Server Synced)",
                "ArcherSkeletonNecromancyLevelIncrease",
                1f, new ConfigDescription(
                    "How much crafting an Archer Skeleton contributes to your Necromancy level increasing.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            MageNecromancyLevelIncrease = plugin.Config.Bind("SkeletonMinion (Server Synced)",
                "MageSkeletonNecromancyLevelIncrease",
                1f, new ConfigDescription(
                    "How much crafting a Poison Skeleton contributes to your Necromancy level increasing.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            MaxSkeletons = plugin.Config.Bind("SkeletonMinion (Server Synced)", "MaximumSkeletons",
                0, new ConfigDescription("The maximum amount of skeletons that can be made (0 = unlimited).", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            MinionLimitIncrementsEveryXLevels = plugin.Config.Bind("SkeletonMinion (Server Synced)",
                "MinionLimitIncrementsEveryXLevels",
                10, new ConfigDescription(
                    "Attention: has no effect if minion limits are off. Increases player's maximum minion count by 1 every X levels. For example, if the limit is 3 skeletons and this is set to 10, then at level 10 Necromancy the player can have 4 minions. Then 5 at level 20, and so on.", null,
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
            
            if (!TryGetComponent(out Humanoid humanoid))
            {
                Logger.LogError("Humanoid component missing!");
                yield break;
            }

            // VisEquipment remembers what armor the skeleton is wearing.
            // Exploit this to reapply the armor so the armor values work
            // again.
            var equipmentHashes = new List<int>()
            {
                humanoid.m_visEquipment.m_currentChestItemHash,
                humanoid.m_visEquipment.m_currentLegItemHash,
                humanoid.m_visEquipment.m_currentHelmetItemHash
            };
            equipmentHashes.ForEach(hash =>
            {
                ZNetScene.instance.GetPrefab(hash);

                var equipmentPrefab = ZNetScene.instance.GetPrefab(hash);
                if (equipmentPrefab != null)
                {
                    humanoid.GiveDefaultItem(equipmentPrefab);
                }
            });

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

        public virtual void ScaleStats(float necromancyLevel)
        {
            var character = GetComponent<Character>();
            if (character == null)
            {
                Logger.LogError("ScaleStats: Character component is null!");
                return;
            }

            var health = SkeletonWand.SkeletonBaseHealth.Value + necromancyLevel * SkeletonWand.SkeletonHealthMultiplier.Value;
            character.SetMaxHealth(health);
            character.SetHealth(health);
        }

        public virtual void ScaleEquipment(float necromancyLevel, SkeletonType skeletonType, ArmorType armorType)
        {
            var defaultItems = new List<GameObject>();

            var humanoid = GetComponent<Humanoid>();
            if (humanoid == null)
            {
                Logger.LogError("ScaleEquipment: humanoid is null!");
                return;
            }

            GameObject GetHelmetPrefab()
            {
                if (skeletonType is SkeletonType.MageTier1 or SkeletonType.MageTier2 or SkeletonType.MageTier3)
                {
                    return ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonMageCirclet");
                }
                if (skeletonType is SkeletonType.PoisonTier1 or SkeletonType.PoisonTier2 or SkeletonType.PoisonTier3)
                {
                    return ZNetScene.instance.GetPrefab(armorType switch
                    {
                        ArmorType.Leather => "ChebGonaz_SkeletonHelmetLeatherPoison",
                        ArmorType.Bronze => "ChebGonaz_SkeletonHelmetBronzePoison",
                        ArmorType.Iron => "ChebGonaz_SkeletonHelmetIronPoison",
                        _ => "ChebGonaz_HelmetBlackIronSkeletonPoison",
                    });
                }
                return ZNetScene.instance.GetPrefab(armorType switch
                {
                    ArmorType.Leather => "ChebGonaz_SkeletonHelmetLeather",
                    ArmorType.Bronze => "ChebGonaz_SkeletonHelmetBronze",
                    ArmorType.Iron => "ChebGonaz_SkeletonHelmetIron",
                    _ => "ChebGonaz_HelmetBlackIronSkeleton",
                });
            }

            // note: as of 1.2.0 weapons were moved into skeleton prefab variants
            // with different m_randomWeapons set. This is because trying to set
            // dynamically seems very difficult -> skeletons forgetting their weapons
            // on logout/log back in; skeletons thinking they have no weapons
            // and running away from enemies.
            //
            // Fortunately, armor seems to work fine.
            switch (armorType)
            {
                case ArmorType.Leather:
                    defaultItems.AddRange(new GameObject[] {
                        GetHelmetPrefab(),
                        ZNetScene.instance.GetPrefab("ArmorLeatherChest"),
                        ZNetScene.instance.GetPrefab("ArmorLeatherLegs"),
                        ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                    if (BasePlugin.DurabilityDamage.Value) { Player.m_localPlayer.GetRightItem().m_durability -= BasePlugin.DurabilityDamageLeather.Value; }
                    break;
                case ArmorType.Bronze:
                    defaultItems.AddRange(new GameObject[] {
                        GetHelmetPrefab(),
                        ZNetScene.instance.GetPrefab("ArmorBronzeChest"),
                        ZNetScene.instance.GetPrefab("ArmorBronzeLegs"),
                        ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                    if (BasePlugin.DurabilityDamage.Value) { Player.m_localPlayer.GetRightItem().m_durability -= BasePlugin.DurabilityDamageBronze.Value; }
                    break;
                case ArmorType.Iron:
                    defaultItems.AddRange(new GameObject[] {
                        GetHelmetPrefab(),
                        ZNetScene.instance.GetPrefab("ArmorIronChest"),
                        ZNetScene.instance.GetPrefab("ArmorIronLegs"),
                        ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                    if (BasePlugin.DurabilityDamage.Value) { Player.m_localPlayer.GetRightItem().m_durability -= BasePlugin.DurabilityDamageIron.Value; }
                    break;
                case ArmorType.BlackMetal:
                    defaultItems.AddRange(new GameObject[] {
                        GetHelmetPrefab(),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorBlackIronChest"),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorBlackIronLegs"),
                        ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                    if (BasePlugin.DurabilityDamage.Value) { Player.m_localPlayer.GetRightItem().m_durability -= BasePlugin.DurabilityDamageBlackIron.Value; }
                    break;
            }

            humanoid.m_defaultItems = defaultItems.ToArray();

            humanoid.GiveDefaultItems();

            if (BasePlugin.DurabilityDamage.Value)
            {
                switch (skeletonType)
                {
                    case SkeletonType.ArcherTier1:
                    case SkeletonType.ArcherTier2:
                    case SkeletonType.ArcherTier3:
                        Player.m_localPlayer.GetRightItem().m_durability -= BasePlugin.DurabilityDamageArcher.Value;
                        break;
                    case SkeletonType.MageTier1:
                    case SkeletonType.MageTier2:
                    case SkeletonType.MageTier3:
                        Player.m_localPlayer.GetRightItem().m_durability -= BasePlugin.DurabilityDamageMage.Value;
                        break;
                    case SkeletonType.PoisonTier1:
                    case SkeletonType.PoisonTier2:
                    case SkeletonType.PoisonTier3:
                        Player.m_localPlayer.GetRightItem().m_durability -= BasePlugin.DurabilityDamagePoison.Value;
                        break;
                    default:
                        Player.m_localPlayer.GetRightItem().m_durability -= BasePlugin.DurabilityDamageWarrior.Value;
                        break;
                }
            }
        }
        
        public static void InstantiateSkeleton(int quality, float playerNecromancyLevel,
            SkeletonType skeletonType, ArmorType armorType)
        {
            if (skeletonType is SkeletonType.None) return;
            
            var player = Player.m_localPlayer;
            var prefabName = InternalName.GetName(skeletonType);
            var prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"{prefabName} does not exist");
                Logger.LogError($"InstantiateSkeleton: spawning {prefabName} failed");
                return;
            }

            var transform = player.transform;
            var spawnedChar = Instantiate(prefab,
                transform.position + transform.forward * 2f + Vector3.up, Quaternion.identity);
            var character = spawnedChar.GetComponent<Character>();
            character.SetLevel(quality);

            spawnedChar.AddComponent<FreshMinion>();

            var minion = skeletonType switch
            {
                SkeletonType.PoisonTier1
                    or SkeletonType.PoisonTier2
                    or SkeletonType.PoisonTier3 => spawnedChar.AddComponent<PoisonSkeletonMinion>(),
                SkeletonType.Woodcutter => spawnedChar.AddComponent<SkeletonWoodcutterMinion>(),
                SkeletonType.Miner => spawnedChar.AddComponent<SkeletonMinerMinion>(),
                _ => spawnedChar.AddComponent<SkeletonMinion>()
            };
            minion.SetCreatedAtLevel(playerNecromancyLevel);
            minion.ScaleEquipment(playerNecromancyLevel, skeletonType, armorType);
            minion.ScaleStats(playerNecromancyLevel);

            if (skeletonType != SkeletonType.Woodcutter
                && skeletonType != SkeletonType.Miner)
            {
                if (Wand.FollowByDefault.Value)
                {
                    minion.Follow(player.gameObject);
                }
                else
                {
                    minion.Wait(player.transform.position);
                }
            }

            var levelIncrease = skeletonType switch
            {
                SkeletonType.ArcherTier1
                    or SkeletonType.ArcherTier2
                    or SkeletonType.ArcherTier3 => ArcherNecromancyLevelIncrease.Value,
                SkeletonType.MageTier1
                    or SkeletonType.MageTier2
                    or SkeletonType.MageTier3 => MageNecromancyLevelIncrease.Value,
                SkeletonType.PoisonTier1
                    or SkeletonType.PoisonTier2
                    or SkeletonType.PoisonTier3 => PoisonNecromancyLevelIncrease.Value,
                _ => NecromancyLevelIncrease.Value
            };
            
            player.RaiseSkill(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill,
                levelIncrease);

            minion.UndeadMinionMaster = player.GetPlayerName();

            // handle refunding of resources on death
            if (DropOnDeath.Value != DropType.Nothing)
            {
                var characterDrop = minion.gameObject.AddComponent<CharacterDrop>();

                if (DropOnDeath.Value == DropType.Everything
                    && SkeletonWand.BoneFragmentsRequiredConfig.Value > 0)
                {
                    // bones
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("BoneFragments"),
                        m_onePerPlayer = true,
                        m_amountMin = SkeletonWand.BoneFragmentsRequiredConfig.Value,
                        m_amountMax = SkeletonWand.BoneFragmentsRequiredConfig.Value,
                        m_chance = 1f
                    });
                }

                if (skeletonType == SkeletonType.Miner)
                {
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("HardAntler"),
                        m_onePerPlayer = true,
                        m_amountMin = SkeletonWand.MinerSkeletonAntlerRequiredConfig.Value,
                        m_amountMax = SkeletonWand.MinerSkeletonAntlerRequiredConfig.Value,
                        m_chance = 1f
                    });
                }

                if (skeletonType == SkeletonType.Woodcutter)
                {
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("Flint"),
                        m_onePerPlayer = true,
                        m_amountMin = SkeletonWand.WoodcutterSkeletonFlintRequiredConfig.Value,
                        m_amountMax = SkeletonWand.WoodcutterSkeletonFlintRequiredConfig.Value,
                        m_chance = 1f
                    });
                }

                if (skeletonType is SkeletonType.MageTier1
                    or SkeletonType.MageTier2
                    or SkeletonType.MageTier3)
                {
                    // surtling core
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("SurtlingCore"),
                        m_onePerPlayer = true,
                        m_amountMin = BasePlugin.SurtlingCoresRequiredConfig.Value,
                        m_amountMax = BasePlugin.SurtlingCoresRequiredConfig.Value,
                        m_chance = 1f
                    });
                }

                switch (armorType)
                {
                    case ArmorType.Leather:
                        characterDrop.m_drops.Add(new CharacterDrop.Drop
                        {
                            // flip a coin for deer or scraps
                            m_prefab = Random.value > .5f
                                ? ZNetScene.instance.GetPrefab("DeerHide")
                                : ZNetScene.instance.GetPrefab("LeatherScraps"),
                            m_onePerPlayer = true,
                            m_amountMin = BasePlugin.ArmorLeatherScrapsRequiredConfig.Value,
                            m_amountMax = BasePlugin.ArmorLeatherScrapsRequiredConfig.Value,
                            m_chance = 1f
                        });
                        break;
                    case ArmorType.Bronze:
                        characterDrop.m_drops.Add(new CharacterDrop.Drop
                        {
                            m_prefab = ZNetScene.instance.GetPrefab("Bronze"),
                            m_onePerPlayer = true,
                            m_amountMin = BasePlugin.ArmorBronzeRequiredConfig.Value,
                            m_amountMax = BasePlugin.ArmorBronzeRequiredConfig.Value,
                            m_chance = 1f
                        });
                        break;
                    case ArmorType.Iron:
                        characterDrop.m_drops.Add(new CharacterDrop.Drop
                        {
                            m_prefab = ZNetScene.instance.GetPrefab("Iron"),
                            m_onePerPlayer = true,
                            m_amountMin = BasePlugin.ArmorIronRequiredConfig.Value,
                            m_amountMax = BasePlugin.ArmorIronRequiredConfig.Value,
                            m_chance = 1f
                        });
                        break;
                    case ArmorType.BlackMetal:
                        characterDrop.m_drops.Add(new CharacterDrop.Drop
                        {
                            m_prefab = ZNetScene.instance.GetPrefab("BlackMetal"),
                            m_onePerPlayer = true,
                            m_amountMin = BasePlugin.ArmorBlackIronRequiredConfig.Value,
                            m_amountMax = BasePlugin.ArmorBlackIronRequiredConfig.Value,
                            m_chance = 1f
                        });
                        break;
                }

                // the component won't be remembered by the game on logout because
                // only what is on the prefab is remembered. Even changes to the prefab
                // aren't remembered. So we must write what we're dropping into
                // the ZDO as well and then read & restore this on Awake
                minion.RecordDrops(characterDrop);
            }
        }
        
        public static void ConsumeResources(SkeletonType skeletonType, ArmorType armorType)
        {
            var player = Player.m_localPlayer;

            // consume bones
            player.GetInventory().RemoveItem("$item_bonefragments", SkeletonWand.BoneFragmentsRequiredConfig.Value);

            // consume other
            switch (skeletonType)
            {
                case SkeletonType.Miner:
                    player.GetInventory().RemoveItem("$item_hardantler", SkeletonWand.MinerSkeletonAntlerRequiredConfig.Value);
                    break;
                case SkeletonType.Woodcutter:
                    player.GetInventory().RemoveItem("$item_flint", SkeletonWand.WoodcutterSkeletonFlintRequiredConfig.Value);
                    break;

                case SkeletonType.ArcherTier1:
                    player.GetInventory()
                        .RemoveItem("$item_arrow_wood", BasePlugin.ArcherTier3ArrowsRequiredConfig.Value);
                    break;
                case SkeletonType.ArcherTier2:
                    player.GetInventory().RemoveItem("$item_arrow_bronze",
                        BasePlugin.ArcherTier3ArrowsRequiredConfig.Value);
                    break;
                case SkeletonType.ArcherTier3:
                    player.GetInventory()
                        .RemoveItem("$item_arrow_iron", BasePlugin.ArcherTier3ArrowsRequiredConfig.Value);
                    break;
                case SkeletonType.ArcherPoison:
                    player.GetInventory().RemoveItem("$item_arrow_poison", BasePlugin.ArcherPoisonArrowsRequiredConfig.Value);
                    break;
                case SkeletonType.ArcherFire:
                    player.GetInventory().RemoveItem("$item_arrow_fire", BasePlugin.ArcherFireArrowsRequiredConfig.Value);
                    break;
                case SkeletonType.ArcherFrost:
                    player.GetInventory().RemoveItem("$item_arrow_frost", BasePlugin.ArcherFrostArrowsRequiredConfig.Value);
                    break;
                case SkeletonType.ArcherSilver:
                    player.GetInventory().RemoveItem("$item_arrow_silver", BasePlugin.ArcherSilverArrowsRequiredConfig.Value);
                    break;
                
                case SkeletonType.WarriorNeedle:
                    player.GetInventory().RemoveItem("$item_needle", BasePlugin.NeedlesRequiredConfig.Value);
                    break;

                case SkeletonType.MageTier1:
                case SkeletonType.MageTier2:
                case SkeletonType.MageTier3:
                    player.GetInventory()
                        .RemoveItem("$item_surtlingcore", BasePlugin.SurtlingCoresRequiredConfig.Value);
                    break;

                case SkeletonType.PoisonTier1:
                case SkeletonType.PoisonTier2:
                case SkeletonType.PoisonTier3:
                    player.GetInventory().RemoveItem("$item_guck", SkeletonWand.PoisonSkeletonGuckRequiredConfig.Value);
                    break;
            }

            // consume armor materials
            switch (armorType)
            {
                case ArmorType.Leather:
                    // todo: expose these options to config
                    var leatherItemTypes = new List<string>()
                    {
                        "$item_leatherscraps",
                        "$item_deerhide",
                        "$item_trollhide",
                        "$item_wolfpelt",
                        "$item_loxpelt",
                        "$item_scalehide"
                    };
                    
                    foreach (var leatherItem in leatherItemTypes)
                    {
                        var leatherItemsInInventory = player.GetInventory().CountItems(leatherItem);
                        if (leatherItemsInInventory >= BasePlugin.ArmorLeatherScrapsRequiredConfig.Value)
                        {
                            player.GetInventory().RemoveItem(leatherItem,
                                BasePlugin.ArmorLeatherScrapsRequiredConfig.Value);
                            break;
                        }
                    }
                    break;
                case ArmorType.Bronze:
                    player.GetInventory().RemoveItem("$item_bronze", BasePlugin.ArmorBronzeRequiredConfig.Value);
                    break;
                case ArmorType.Iron:
                    player.GetInventory().RemoveItem("$item_iron", BasePlugin.ArmorIronRequiredConfig.Value);
                    break;
                case ArmorType.BlackMetal:
                    player.GetInventory().RemoveItem("$item_blackmetal", BasePlugin.ArmorBlackIronRequiredConfig.Value);
                    break;
            }
        }
        
        public static void CountActiveSkeletonMinions()
        {
            //todo: this function is poorly designed. Return value is not
            // important to its function; function has side effects, etc.
            // Refactor sometime

            int result = 0;
            // based off BaseAI.FindClosestCreature
            var allCharacters = Character.GetAllCharacters();
            var minionsFound = new List<Tuple<int, Character>>();

            foreach (var item in allCharacters)
            {
                if (item.IsDead())
                {
                    continue;
                }

                var minion = item.GetComponent<SkeletonMinion>();
                if (minion != null
                    && minion.BelongsToPlayer(Player.m_localPlayer.GetPlayerName()))
                {
                    minionsFound.Add(new Tuple<int, Character>(minion.createdOrder, item));
                }
            }

            // reverse so that we get newest first, oldest last. This means
            // when we kill off surplus, the oldest things are getting killed
            // not the newest things
            minionsFound = minionsFound.OrderByDescending((arg) => arg.Item1).ToList();
            
            var playerNecromancyLevel =
                Player.m_localPlayer.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill);
            var bonusMinions = MinionLimitIncrementsEveryXLevels.Value > 0
                ? (int)playerNecromancyLevel / MinionLimitIncrementsEveryXLevels.Value
                : 0;
            var maxMinions = MaxSkeletons.Value + bonusMinions;

            foreach (var t in minionsFound)
            {
                // kill off surplus
                if (result >= maxMinions - 1)
                {
                    Tuple<int, Character> tuple = t;
                    tuple.Item2.SetHealth(0);
                    continue;
                }

                result++;
            }
        }
    }
}
