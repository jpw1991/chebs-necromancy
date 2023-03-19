using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using ChebsNecromancy.Items;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Minions
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

        // for limits checking
        private static int _createdOrderIncrementer;

        public static ConfigEntry<DropType> DropOnDeath;
        public static ConfigEntry<bool> PackDropItemsIntoCargoCrate;
        
        public static ConfigEntry<int> MaxDraugr;
        public static ConfigEntry<int> MinionLimitIncrementsEveryXLevels;

        public new static void CreateConfigs(BaseUnityPlugin plugin)
        {
            DropOnDeath = plugin.Config.Bind("DraugrMinion (Server Synced)", "DropOnDeath",
                DropType.JustResources, new ConfigDescription("Whether a minion refunds anything when it dies.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            PackDropItemsIntoCargoCrate = plugin.Config.Bind("DraugrMinion (Server Synced)",
                "PackDroppedItemsIntoCargoCrate",
                true, new ConfigDescription(
                    "If set to true, dropped items will be packed into a cargo crate. This means they won't sink in water, which is useful for more valuable drops like Surtling Cores and metal ingots.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            MaxDraugr = plugin.Config.Bind("DraugrMinion (Server Synced)", "MaximumDraugr",
                0, new ConfigDescription("The maximum Draugr allowed to be created (0 = unlimited).", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            MinionLimitIncrementsEveryXLevels = plugin.Config.Bind("DraugrMinion (Server Synced)",
                "MinionLimitIncrementsEveryXLevels",
                10, new ConfigDescription(
                    "Attention: has no effect if minion limits are off. Increases player's maximum minion count by 1 every X levels. For example, if the limit is 3 draugr and this is set to 10, then at level 10 Necromancy the player can have 4 minions. Then 5 at level 20, and so on.", null,
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

            // VisEquipment remembers what armor the draugr is wearing.
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
                    //Jotunn.Logger.LogInfo($"Giving default item {equipmentPrefab.name}");
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

        public void ScaleStats(float necromancyLevel)
        {
            Character character = GetComponent<Character>();
            if (character == null)
            {
                Logger.LogError("ScaleStats: Character component is null!");
                return;
            }

            float health = DraugrWand.DraugrBaseHealth.Value +
                           necromancyLevel * DraugrWand.DraugrHealthMultiplier.Value;
            character.SetMaxHealth(health);
            character.SetHealth(health);
        }

        public virtual void ScaleEquipment(float necromancyLevel, ArmorType armorType)
        {
            List<GameObject> defaultItems = new List<GameObject>();

            Humanoid humanoid = GetComponent<Humanoid>();
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
    }
}