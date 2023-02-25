using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using ChebsNecromancy.Items;
using UnityEngine;
using Logger = Jotunn.Logger;

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
            [InternalName("ChebGonaz_SkeletonMage")] MageTier1,
            [InternalName("ChebGonaz_SkeletonMageTier2")] MageTier2,
            [InternalName("ChebGonaz_SkeletonMageTier3")] MageTier3,
            [InternalName("ChebGonaz_PoisonSkeleton")] PoisonTier1,
            [InternalName("ChebGonaz_PoisonSkeleton2")] PoisonTier2,
            [InternalName("ChebGonaz_PoisonSkeleton3")] PoisonTier3,
            [InternalName("ChebGonaz_SkeletonWoodcutter")] Woodcutter,
            [InternalName("ChebGonaz_SkeletonMiner")] Miner,
        };

        // for limits checking
        private static int _createdOrderIncrementer;
        public int createdOrder;

        public static ConfigEntry<DropType> DropOnDeath;
        public static ConfigEntry<bool> PackDropItemsIntoCargoCrate;

        public new static void CreateConfigs(BaseUnityPlugin plugin)
        {
            DropOnDeath = plugin.Config.Bind("SkeletonMinion (Server Synced)", "DropOnDeath",
                DropType.JustResources, new ConfigDescription("Whether a minion refunds anything when it dies.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            PackDropItemsIntoCargoCrate = plugin.Config.Bind("SkeletonMinion (Server Synced)", "PackDroppedItemsIntoCargoCrate",
                true, new ConfigDescription("If set to true, dropped items will be packed into a cargo crate. This means they won't sink in water, which is useful for more valuable drops like Surtling Cores and metal ingots.", null,
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
            List<int> equipmentHashes = new List<int>()
            {
                humanoid.m_visEquipment.m_currentChestItemHash,
                humanoid.m_visEquipment.m_currentLegItemHash,
                humanoid.m_visEquipment.m_currentHelmetItemHash
            };
            equipmentHashes.ForEach(hash =>
            {
                ZNetScene.instance.GetPrefab(hash);

                GameObject equipmentPrefab = ZNetScene.instance.GetPrefab(hash);
                if (equipmentPrefab != null)
                {
                    humanoid.GiveDefaultItem(equipmentPrefab);
                }
            });

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

        public virtual void ScaleStats(float necromancyLevel)
        {
            Character character = GetComponent<Character>();
            if (character == null)
            {
                Logger.LogError("ScaleStats: Character component is null!");
                return;
            }

            float health = SkeletonWand.SkeletonBaseHealth.Value + necromancyLevel * SkeletonWand.SkeletonHealthMultiplier.Value;
            character.SetMaxHealth(health);
            character.SetHealth(health);
        }

        public virtual void ScaleEquipment(float necromancyLevel, SkeletonType skeletonType, ArmorType armorType)
        {
            List<GameObject> defaultItems = new List<GameObject>();

            Humanoid humanoid = GetComponent<Humanoid>();
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
    }
}
