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
            Warrior,
            Archer,
        };

        // for limits checking
        private static int _createdOrderIncrementer;
        public int createdOrder;

        public static ConfigEntry<DropType> DropOnDeath;
        public static ConfigEntry<bool> PackDropItemsIntoCargoCrate;

        public new static void CreateConfigs(BaseUnityPlugin plugin)
        { 
            DropOnDeath = plugin.Config.Bind("DraugrMinion (Server Synced)", "DropOnDeath",
                DropType.JustResources, new ConfigDescription("Whether a minion refunds anything when it dies.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            PackDropItemsIntoCargoCrate = plugin.Config.Bind("DraugrMinion (Server Synced)", "PackDroppedItemsIntoCargoCrate",
                true, new ConfigDescription("If set to true, dropped items will be packed into a cargo crate. This means they won't sink in water, which is useful for more valuable drops like Surtling Cores and metal ingots.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        public override void Awake()
        {
            base.Awake();

            _createdOrderIncrementer++;
            createdOrder = _createdOrderIncrementer;

            StartCoroutine(WaitForLocalPlayer());
        }

        IEnumerator WaitForLocalPlayer()
        {
            yield return new WaitUntil(() => Player.m_localPlayer != null);

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
            float health = DraugrWand.DraugrBaseHealth.Value + necromancyLevel * DraugrWand.DraugrHealthMultiplier.Value;
            character.SetMaxHealth(health);
            character.SetHealth(health);
        }

        public virtual void ScaleEquipment(float necromancyLevel, bool leatherArmor, bool bronzeArmor, bool ironArmor, bool blackIronArmor)
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
            if (leatherArmor)
            {
                defaultItems.AddRange(new GameObject[] {
                    ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetLeather"),
                    ZNetScene.instance.GetPrefab("ArmorLeatherChest"),
                    ZNetScene.instance.GetPrefab("ArmorLeatherLegs"),
                    //ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                if (DraugrWand.DurabilityDamage.Value) { Player.m_localPlayer.GetRightItem().m_durability -= DraugrWand.DurabilityDamageLeather.Value; }
            }
            else if (bronzeArmor)
            {
                defaultItems.AddRange(new GameObject[] {
                    ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetBronze"),
                    ZNetScene.instance.GetPrefab("ArmorBronzeChest"),
                    ZNetScene.instance.GetPrefab("ArmorBronzeLegs"),
                    //ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                if (DraugrWand.DurabilityDamage.Value) { Player.m_localPlayer.GetRightItem().m_durability -= DraugrWand.DurabilityDamageBronze.Value; }
            }
            else if (ironArmor)
            {
                defaultItems.AddRange(new GameObject[] {
                    ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetIron"),
                    ZNetScene.instance.GetPrefab("ArmorIronChest"),
                    ZNetScene.instance.GetPrefab("ArmorIronLegs"),
                    //ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                if (DraugrWand.DurabilityDamage.Value) { Player.m_localPlayer.GetRightItem().m_durability -= DraugrWand.DurabilityDamageIron.Value; }
            }
            else if (blackIronArmor)
            {
                defaultItems.AddRange(new GameObject[] {
                    ZNetScene.instance.GetPrefab("ChebGonaz_HelmetBlackIronSkeleton"),
                    ZNetScene.instance.GetPrefab("ChebGonaz_ArmorBlackIronChest"),
                    ZNetScene.instance.GetPrefab("ChebGonaz_ArmorBlackIronLegs"),
                    //ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                if (DraugrWand.DurabilityDamage.Value) { Player.m_localPlayer.GetRightItem().m_durability -= DraugrWand.DurabilityDamageBlackIron.Value; }
            }

            humanoid.m_defaultItems = defaultItems.ToArray();

            humanoid.GiveDefaultItems();
        }
    }
}
