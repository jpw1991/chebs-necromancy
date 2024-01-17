using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using ChebsNecromancy.Items.Wands;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.Minions;
using Jotunn.Managers;
using UnityEngine;
using Logger = Jotunn.Logger;
using Random = UnityEngine.Random;

namespace ChebsNecromancy.Minions.Draugr
{
    internal class DraugrMinion : UndeadMinion
    {
        public enum DraugrType
        {
            None,
            [InternalName("ChebGonaz_DraugrWarrior")] WarriorTier1,
            [InternalName("ChebGonaz_DraugrWarriorTier2")] WarriorTier2,
            [InternalName("ChebGonaz_DraugrWarriorTier3")] WarriorTier3,
            [InternalName("ChebGonaz_DraugrWarriorTier4")] WarriorTier4,
            [InternalName("ChebGonaz_DraugrArcher")] ArcherTier1,
            [InternalName("ChebGonaz_DraugrArcherTier2")] ArcherTier2,
            [InternalName("ChebGonaz_DraugrArcherTier3")] ArcherTier3,
            [InternalName("ChebGonaz_DraugrArcherPoison")] ArcherPoison,
            [InternalName("ChebGonaz_DraugrArcherFire")] ArcherFire,
            [InternalName("ChebGonaz_DraugrArcherFrost")] ArcherFrost,
            [InternalName("ChebGonaz_DraugrArcherSilver")] ArcherSilver,
            [InternalName("ChebGonaz_DraugrWarriorNeedle")] WarriorNeedle,
        };
        
        private static List<int> _hashList;

        public static bool IsDraugrHash(int hash)
        {
            if (_hashList == null)
            {
                _hashList = new List<int>();
                foreach (DraugrType value in Enum.GetValues(typeof(DraugrType)))
                {
                    _hashList.Add(InternalName.GetName(value).GetHashCode());
                }
            }

            return _hashList.Contains(hash);
        }

        // for limits checking
        private static int _createdOrderIncrementer;

        public static ConfigEntry<int> MaxDraugr;
        public static ConfigEntry<int> MinionLimitIncrementsEveryXLevels;
        public static ConfigEntry<bool> IdleSoundsEnabled;

        public static ConfigEntry<float> NecromancyLevelIncrease, ArcherNecromancyLevelIncrease;

        public new static void CreateConfigs(BaseUnityPlugin plugin)
        {
            const string serverSynced = "DraugrMinion (Server Synced)";
            const string client = "DraugrMinion (Client)";
            MaxDraugr = plugin.Config.Bind(serverSynced, "MaximumDraugr",
                0, new ConfigDescription("The maximum Draugr allowed to be created (0 = unlimited).", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            MinionLimitIncrementsEveryXLevels = plugin.Config.Bind(serverSynced,
                "MinionLimitIncrementsEveryXLevels",
                10, new ConfigDescription(
                    "Attention: has no effect if minion limits are off. Increases player's maximum minion count by 1 every X levels. For example, if the limit is 3 draugr and this is set to 10, then at level 10 Necromancy the player can have 4 minions. Then 5 at level 20, and so on.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            NecromancyLevelIncrease = plugin.Config.Bind(serverSynced, "DraugrNecromancyLevelIncrease",
                1.5f, new ConfigDescription(
                    "How much creating a Draugr contributes to your Necromancy level increasing.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            ArcherNecromancyLevelIncrease = plugin.Config.Bind(serverSynced, "DraugrArcherNecromancyLevelIncrease",
                2f, new ConfigDescription(
                    "How much creating a Draugr Archer contributes to your Necromancy level increasing.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            IdleSoundsEnabled = plugin.Config.Bind(client, "IdleSoundsEnabled",
                true, new ConfigDescription("Set to false to make the draugr quiet."));
        }

        public override void Awake()
        {
            base.Awake();

            _createdOrderIncrementer++;
            createdOrder = _createdOrderIncrementer;

            StartCoroutine(WaitForZNet());

            if (!IdleSoundsEnabled.Value && TryGetComponent(out MonsterAI monsterAI))
            {
                foreach (var effectPrefab in monsterAI.m_idleSound.m_effectPrefabs)
                {
                    effectPrefab.m_enabled = false;
                }
            }
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

            // VisEquipment remembers what armor the draugr is wearing.
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
                    //Jotunn.Logger.LogInfo($"Giving default item {equipmentPrefab.name}");
                    humanoid.GiveDefaultItem(equipmentPrefab);
                }
            });

            RestoreDrops();
            
            LoadEyeMaterial();

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

            var health = DraugrWand.DraugrBaseHealth.Value +
                         necromancyLevel * DraugrWand.DraugrHealthMultiplier.Value;
            character.SetMaxHealth(health);
            character.SetHealth(health);
        }

        public virtual void ScaleEquipment(float necromancyLevel, ArmorType armorType)
        {
            var defaultItems = new List<GameObject>();

            var humanoid = GetComponent<Humanoid>();
            if (humanoid == null)
            {
                Logger.LogError("ScaleEquipment: humanoid is null!");
                return;
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
                    defaultItems.AddRange(new GameObject[]
                    {
                        ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetLeather"),
                        ZNetScene.instance.GetPrefab("ArmorLeatherChest"),
                        ZNetScene.instance.GetPrefab("ArmorLeatherLegs"),
                        //ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                    if (BasePlugin.DurabilityDamage.Value)
                    {
                        Player.m_localPlayer.GetRightItem().m_durability -= BasePlugin.DurabilityDamageLeather.Value;
                    }

                    break;
                case ArmorType.LeatherTroll:
                    defaultItems.AddRange(new GameObject[]
                    {
                        ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetLeatherTroll"),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorLeatherChestTroll"),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorLeatherLegsTroll"),
                        //ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                    if (BasePlugin.DurabilityDamage.Value)
                    {
                        Player.m_localPlayer.GetRightItem().m_durability -= BasePlugin.DurabilityDamageLeather.Value;
                    }

                    break;
                case ArmorType.LeatherWolf:
                    defaultItems.AddRange(new GameObject[]
                    {
                        ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetLeatherWolf"),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorLeatherChestWolf"),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorLeatherLegsWolf"),
                        //ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                    if (BasePlugin.DurabilityDamage.Value)
                    {
                        Player.m_localPlayer.GetRightItem().m_durability -= BasePlugin.DurabilityDamageLeather.Value;
                    }

                    break;
                case ArmorType.LeatherLox:
                    defaultItems.AddRange(new GameObject[]
                    {
                        ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetLeatherLox"),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorLeatherChestLox"),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorLeatherLegsLox"),
                        //ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                    if (BasePlugin.DurabilityDamage.Value)
                    {
                        Player.m_localPlayer.GetRightItem().m_durability -= BasePlugin.DurabilityDamageLeather.Value;
                    }

                    break;
                case ArmorType.Bronze:
                    defaultItems.AddRange(new GameObject[]
                    {
                        ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetBronze"),
                        ZNetScene.instance.GetPrefab("ArmorBronzeChest"),
                        ZNetScene.instance.GetPrefab("ArmorBronzeLegs"),
                        //ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                    if (BasePlugin.DurabilityDamage.Value)
                    {
                        Player.m_localPlayer.GetRightItem().m_durability -= BasePlugin.DurabilityDamageBronze.Value;
                    }

                    break;
                case ArmorType.Iron:
                    defaultItems.AddRange(new GameObject[]
                    {
                        ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetIron"),
                        ZNetScene.instance.GetPrefab("ArmorIronChest"),
                        ZNetScene.instance.GetPrefab("ArmorIronLegs"),
                        //ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                    if (BasePlugin.DurabilityDamage.Value)
                    {
                        Player.m_localPlayer.GetRightItem().m_durability -= BasePlugin.DurabilityDamageIron.Value;
                    }

                    break;
                case ArmorType.BlackMetal:
                    defaultItems.AddRange(new GameObject[]
                    {
                        ZNetScene.instance.GetPrefab("ChebGonaz_HelmetBlackIronSkeleton"),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorBlackIronChest"),
                        ZNetScene.instance.GetPrefab("ChebGonaz_ArmorBlackIronLegs"),
                        //ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                    if (BasePlugin.DurabilityDamage.Value)
                    {
                        Player.m_localPlayer.GetRightItem().m_durability -= BasePlugin.DurabilityDamageBlackIron.Value;
                    }

                    break;
            }

            humanoid.m_defaultItems = defaultItems.ToArray();

            humanoid.GiveDefaultItems();
        }
        
        public static void ConsumeResources(DraugrType draugrType, ArmorType armorType)
        {
            var inventory = Player.m_localPlayer.GetInventory();
            
            switch (draugrType)
            {
                case DraugrType.ArcherTier1:
                    ConsumeRequirements(DraugrArcherTier1Minion.ItemsCost, inventory);
                    break;
                case DraugrType.ArcherTier2:
                    ConsumeRequirements(DraugrArcherTier2Minion.ItemsCost, inventory);
                    break;
                case DraugrType.ArcherTier3:
                    ConsumeRequirements(DraugrArcherTier3Minion.ItemsCost, inventory);
                    break;
                case DraugrType.ArcherPoison:
                    ConsumeRequirements(DraugrArcherPoisonMinion.ItemsCost, inventory);
                    break;
                case DraugrType.ArcherFire:
                    ConsumeRequirements(DraugrArcherFireMinion.ItemsCost, inventory);
                    break;
                case DraugrType.ArcherFrost:
                    ConsumeRequirements(DraugrArcherFrostMinion.ItemsCost, inventory);
                    break;
                case DraugrType.ArcherSilver:
                    ConsumeRequirements(DraugrArcherSilverMinion.ItemsCost, inventory);
                    break;
                
                case DraugrType.WarriorNeedle:
                    ConsumeRequirements(DraugrWarriorMinion.ItemsCost, inventory);
                    inventory.RemoveItem("$item_needle", BasePlugin.NeedlesRequiredConfig.Value);
                    break;
                
                default:
                    ConsumeRequirements(DraugrWarriorMinion.ItemsCost, inventory);
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
                        "$item_scalehide"
                    };
                    
                    foreach (var leatherItem in leatherItemTypes)
                    {
                        var leatherItemsInInventory = inventory.CountItems(leatherItem);
                        if (leatherItemsInInventory >= BasePlugin.ArmorLeatherScrapsRequiredConfig.Value)
                        {
                            inventory.RemoveItem(leatherItem,
                                BasePlugin.ArmorLeatherScrapsRequiredConfig.Value);
                            break;
                        }
                    }
                    break;
                case ArmorType.LeatherTroll:
                    inventory.RemoveItem("$item_trollhide", BasePlugin.ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                case ArmorType.LeatherWolf:
                    inventory.RemoveItem("$item_wolfpelt", BasePlugin.ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                case ArmorType.LeatherLox:
                    inventory.RemoveItem("$item_loxpelt", BasePlugin.ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                case ArmorType.Bronze:
                    inventory.RemoveItem("$item_bronze", BasePlugin.ArmorBronzeRequiredConfig.Value);
                    break;
                case ArmorType.Iron:
                    inventory.RemoveItem("$item_iron", BasePlugin.ArmorIronRequiredConfig.Value);
                    break;
                case ArmorType.BlackMetal:
                    inventory.RemoveItem("$item_blackmetal", BasePlugin.ArmorBlackIronRequiredConfig.Value);
                    break;
            }
        }
        
        public static void InstantiateDraugr(int quality, float playerNecromancyLevel,
            DraugrType skeletonType, ArmorType armorType)
        {
            if (skeletonType is DraugrType.None) return;
            
            var player = Player.m_localPlayer;
            var prefabName = InternalName.GetName(skeletonType);
            var prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"{prefabName} does not exist");
                Logger.LogError($"InstantiateDraugr: spawning {prefabName} failed");
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
                DraugrType.WarriorNeedle
                    or DraugrType.WarriorTier1
                    or DraugrType.WarriorTier2
                    or DraugrType.WarriorTier3
                    or DraugrType.WarriorTier4 => spawnedChar.AddComponent<DraugrWarriorMinion>(),
                DraugrType.ArcherFire => spawnedChar.AddComponent<DraugrArcherFireMinion>(),
                DraugrType.ArcherFrost => spawnedChar.AddComponent<DraugrArcherFrostMinion>(),
                DraugrType.ArcherPoison => spawnedChar.AddComponent<DraugrArcherPoisonMinion>(),
                DraugrType.ArcherSilver => spawnedChar.AddComponent<DraugrArcherSilverMinion>(),
                DraugrType.ArcherTier1 => spawnedChar.AddComponent<DraugrArcherTier1Minion>(),
                DraugrType.ArcherTier2 => spawnedChar.AddComponent<DraugrArcherTier2Minion>(),
                DraugrType.ArcherTier3 => spawnedChar.AddComponent<DraugrArcherTier3Minion>(),
                _ => spawnedChar.AddComponent<DraugrMinion>()
            };
            minion.SetCreatedAtLevel(playerNecromancyLevel);
            minion.ScaleEquipment(playerNecromancyLevel, armorType);
            minion.ScaleStats(playerNecromancyLevel);

            if (Wand.FollowByDefault.Value)
            {
                minion.Follow(player.gameObject);
            }
            else
            {
                minion.Wait(player.transform.position);
            }

            var levelIncrease = skeletonType switch
            {
                DraugrType.ArcherTier1
                    or DraugrType.ArcherTier2
                    or DraugrType.ArcherTier3
                    or DraugrType.ArcherFire
                    or DraugrType.ArcherFrost
                    or DraugrType.ArcherPoison
                    or DraugrType.ArcherSilver => ArcherNecromancyLevelIncrease.Value,
                _ => NecromancyLevelIncrease.Value
            };
            
            player.RaiseSkill(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill,
                levelIncrease);

            minion.UndeadMinionMaster = player.GetPlayerName();

            // handle refunding of resources on death
            if (DropOnDeath.Value == DropType.Nothing) return;
            
            var characterDrop = spawnedChar.AddComponent<CharacterDrop>();
            if (DropOnDeath.Value == DropType.Everything)
            {
                switch (skeletonType)
                {
                    case DraugrType.WarriorTier1:
                    case DraugrType.WarriorTier2:
                    case DraugrType.WarriorTier3:
                    case DraugrType.WarriorTier4:
                    case DraugrType.WarriorNeedle:
                        GenerateDeathDrops(characterDrop, DraugrWarriorMinion.ItemsCost);
                        break;
                    case DraugrType.ArcherTier1:
                        GenerateDeathDrops(characterDrop, DraugrArcherTier1Minion.ItemsCost);
                        break;
                    case DraugrType.ArcherTier2:
                        GenerateDeathDrops(characterDrop, DraugrArcherTier2Minion.ItemsCost);
                        break;
                    case DraugrType.ArcherTier3:
                        GenerateDeathDrops(characterDrop, DraugrArcherTier3Minion.ItemsCost);
                        break;
                    case DraugrType.ArcherPoison:
                        GenerateDeathDrops(characterDrop, DraugrArcherPoisonMinion.ItemsCost);
                        break;
                    case DraugrType.ArcherFire:
                        GenerateDeathDrops(characterDrop, DraugrArcherFireMinion.ItemsCost);
                        break;
                    case DraugrType.ArcherFrost:
                        GenerateDeathDrops(characterDrop, DraugrArcherFrostMinion.ItemsCost);
                        break;
                    case DraugrType.ArcherSilver:
                        GenerateDeathDrops(characterDrop, DraugrArcherSilverMinion.ItemsCost);
                        break;
                }
            }

            switch (armorType)
            {
                case ArmorType.Leather:
                    AddOrUpdateDrop(characterDrop, 
                        Random.value > .5f ? "DeerHide" : "LeatherScraps", // flip a coin for deer or scraps
                        BasePlugin.ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                case ArmorType.LeatherTroll:
                    AddOrUpdateDrop(characterDrop, "TrollHide", BasePlugin.ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                case ArmorType.LeatherWolf:
                    AddOrUpdateDrop(characterDrop, "WolfPelt", BasePlugin.ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                case ArmorType.LeatherLox:
                    AddOrUpdateDrop(characterDrop, "LoxPelt", BasePlugin.ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                case ArmorType.Bronze:
                    AddOrUpdateDrop(characterDrop, "Bronze", BasePlugin.ArmorBronzeRequiredConfig.Value);
                    break;
                case ArmorType.Iron:
                    AddOrUpdateDrop(characterDrop, "Iron", BasePlugin.ArmorIronRequiredConfig.Value);
                    break;
                case ArmorType.BlackMetal:
                    AddOrUpdateDrop(characterDrop, "BlackMetal", BasePlugin.ArmorBlackIronRequiredConfig.Value);
                    break;
            }

            // the component won't be remembered by the game on logout because
            // only what is on the prefab is remembered. Even changes to the prefab
            // aren't remembered. So we must write what we're dropping into
            // the ZDO as well and then read & restore this on Awake
            minion.RecordDrops(characterDrop);
        }
        
        public override void LoadEyeMaterial()
        {
            var eyeColor = Eye;
            if (Eyes.TryGetValue(eyeColor, out var eye))
            {
                var eyeL = transform.Find("Visual/_draugr_base/Armature/Hips/Spine0/Spine1/Spine2/Head/sphere");
                if (eyeL != null && eyeL.TryGetComponent(out MeshRenderer lMeshRenderer))
                {
                    // var mats = lMeshRenderer.materials;
                    // for (var i = 0; i < mats.Length; i++) mats[i] = eye;
                    lMeshRenderer.sharedMaterial = eye;
                }
                else
                {
                    Logger.LogError($"{name} (eyeL={eyeL}) Failed to get mesh renderer for eye_l");
                }
                
                var eyeR = transform.Find("Visual/_draugr_base/Armature/Hips/Spine0/Spine1/Spine2/Head/sphere (1)");
                if (eyeR != null && eyeR.TryGetComponent(out MeshRenderer rMeshRenderer))
                {
                    // var mats = rMeshRenderer.materials;
                    // for (var i = 0; i < mats.Length; i++) mats[i] = eye;
                    rMeshRenderer.sharedMaterial = eye;
                }
                else
                {
                    Logger.LogError($"{name} (eyeR={eyeR}) Failed to get mesh renderer for eye_r");
                }
            }
        }
    }
}