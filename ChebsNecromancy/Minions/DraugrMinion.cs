using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using Jotunn.Managers;
using UnityEngine;
namespace ChebsNecromancy.Minions
{
    internal class DraugrMinion : UndeadMinion
    {
        public enum DraugrType
        {
            Warrior,
            Archer,
        };

        // for limits checking
        private static int createdOrderIncrementer;
        public int createdOrder;

        public static ConfigEntry<DropType> dropOnDeath;
        public static ConfigEntry<bool> packDropItemsIntoCargoCrate;

        public static new void CreateConfigs(BaseUnityPlugin plugin)
        { 
            dropOnDeath = plugin.Config.Bind("DraugrMinion (Server Synced)", "DropOnDeath",
                DropType.JustResources, new ConfigDescription("Whether a minion refunds anything when it dies.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            packDropItemsIntoCargoCrate = plugin.Config.Bind("DraugrMinion (Server Synced)", "PackDroppedItemsIntoCargoCrate",
                true, new ConfigDescription("If set to true, dropped items will be packed into a cargo crate. This means they won't sink in water, which is useful for more valuable drops like Surtling Cores and metal ingots.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        public override void Awake()
        {
            base.Awake();

            createdOrderIncrementer++;
            createdOrder = createdOrderIncrementer;

            StartCoroutine(WaitForLocalPlayer());
        }

        IEnumerator WaitForLocalPlayer()
        {
            while (Player.m_localPlayer == null)
            {
                yield return new WaitForSeconds(1);
            }
            ScaleStats(Player.m_localPlayer.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.necromancySkillIdentifier).m_skill));

            // by the time player arrives, ZNet stuff is certainly ready
            if (TryGetComponent(out Humanoid humanoid))
            {
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
            }

            RestoreDrops();

            WaitAtRecordedPosition();
        }

        public void ScaleStats(float necromancyLevel)
        {
            Character character = GetComponent<Character>();
            if (character == null)
            {
                Jotunn.Logger.LogError("ScaleStats: Character component is null!");
                return;
            }
            float health = DraugrWand.draugrBaseHealth.Value + necromancyLevel * DraugrWand.draugrHealthMultiplier.Value;
            character.SetMaxHealth(health);
            character.SetHealth(health);
        }

        public virtual void ScaleEquipment(float necromancyLevel, bool leatherArmor, bool bronzeArmor, bool ironArmor, bool blackIronArmor)
        {
            List<GameObject> defaultItems = new List<GameObject>();

            Humanoid humanoid = GetComponent<Humanoid>();
            if (humanoid == null)
            {
                Jotunn.Logger.LogError("ScaleEquipment: humanoid is null!");
                return;
            }

            // note: as of 1.2.0 weapons were moved into skeleton prefab variants
            // with different m_randomWeapons set. This is because trying to set
            // dynamically seems very difficult -> skeletons forgetting their weapons
            // on logout/log back in; skeletons thinking they have no weapons
            // and running away from enemies.
            //
            // Fortunately, armor seems to work fine.
            if (leatherArmor)
            {
                defaultItems.AddRange(new GameObject[] {
                    ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetLeather"),
                    ZNetScene.instance.GetPrefab("ArmorLeatherChest"),
                    ZNetScene.instance.GetPrefab("ArmorLeatherLegs"),
                    //ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                if (DraugrWand.durabilityDamage.Value) { Player.m_localPlayer.GetRightItem().m_durability -= DraugrWand.durabilityDamageLeather.Value; }
            }
            else if (bronzeArmor)
            {
                defaultItems.AddRange(new GameObject[] {
                    ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetBronze"),
                    ZNetScene.instance.GetPrefab("ArmorBronzeChest"),
                    ZNetScene.instance.GetPrefab("ArmorBronzeLegs"),
                    //ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                if (DraugrWand.durabilityDamage.Value) { Player.m_localPlayer.GetRightItem().m_durability -= DraugrWand.durabilityDamageBronze.Value; }
            }
            else if (ironArmor)
            {
                defaultItems.AddRange(new GameObject[] {
                    ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetIron"),
                    ZNetScene.instance.GetPrefab("ArmorIronChest"),
                    ZNetScene.instance.GetPrefab("ArmorIronLegs"),
                    //ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                if (DraugrWand.durabilityDamage.Value) { Player.m_localPlayer.GetRightItem().m_durability -= DraugrWand.durabilityDamageIron.Value; }
            }
            else if (blackIronArmor)
            {
                defaultItems.AddRange(new GameObject[] {
                    ZNetScene.instance.GetPrefab("ChebGonaz_HelmetBlackIronSkeleton"),
                    ZNetScene.instance.GetPrefab("ChebGonaz_ArmorBlackIronChest"),
                    ZNetScene.instance.GetPrefab("ChebGonaz_ArmorBlackIronLegs"),
                    //ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                if (DraugrWand.durabilityDamage.Value) { Player.m_localPlayer.GetRightItem().m_durability -= DraugrWand.durabilityDamageBlackIron.Value; }
            }

            humanoid.m_defaultItems = defaultItems.ToArray();

            humanoid.GiveDefaultItems();
        }
    }
}
