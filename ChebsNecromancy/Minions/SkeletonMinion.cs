using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using Jotunn.Managers;
using UnityEngine;

namespace ChebsNecromancy.Minions
{
    internal class SkeletonMinion : UndeadMinion
    {
        public enum SkeletonType
        {
            Warrior,
            Archer,
            Mage,
            Poison,
        };

        // for limits checking
        private static int createdOrderIncrementer;
        public int createdOrder;

        public static ConfigEntry<DropType> dropOnDeath;
        public static ConfigEntry<bool> packDropItemsIntoCargoCrate;

        public static new void CreateConfigs(BaseUnityPlugin plugin)
        {
            dropOnDeath = plugin.Config.Bind("SkeletonMinion (Server Synced)", "DropOnDeath",
                DropType.JustResources, new ConfigDescription("Whether a minion refunds anything when it dies.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            packDropItemsIntoCargoCrate = plugin.Config.Bind("SkeletonMinion (Server Synced)", "PackDroppedItemsIntoCargoCrate",
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
            yield return new WaitUntil(() => Player.m_localPlayer != null);

            ScaleStats(Player.m_localPlayer.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.necromancySkillIdentifier).m_skill));

            // by the time player arrives, ZNet stuff is certainly ready
            if (TryGetComponent(out Humanoid humanoid))
            {
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
                        //Jotunn.Logger.LogInfo($"Giving default item {equipmentPrefab.name}");
                        humanoid.GiveDefaultItem(equipmentPrefab);
                    }
                });
            }

            RestoreDrops();
        }

        public virtual void ScaleStats(float necromancyLevel)
        {
            Character character = GetComponent<Character>();
            if (character == null)
            {
                Jotunn.Logger.LogError("ScaleStats: Character component is null!");
                return;
            }

            // only scale player's skeletons, not other ppls
            if (!BelongsToPlayer(Player.m_localPlayer.GetPlayerName())) return;

            float health = SkeletonWand.skeletonBaseHealth.Value + necromancyLevel * SkeletonWand.skeletonHealthMultiplier.Value;
            character.SetMaxHealth(health);
            character.SetHealth(health);
        }

        public virtual void ScaleEquipment(float necromancyLevel, SkeletonType skeletonType, bool leatherArmor, bool bronzeArmor, bool ironArmor, bool blackIronArmor)
        {
            List<GameObject> defaultItems = new List<GameObject>();

            Humanoid humanoid = GetComponent<Humanoid>();
            if (humanoid == null)
            {
                Jotunn.Logger.LogError("ScaleEquipment: humanoid is null!");
                return;
            }

            GameObject GetHelmetPrefab()
            {
                if (skeletonType == SkeletonType.Mage)
                {
                    return ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonMageCirclet");
                }
                if (skeletonType == SkeletonType.Poison)
                {
                    if (leatherArmor) return ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetLeatherPoison");
                    if (bronzeArmor) return ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetBronzePoison");
                    if (ironArmor) return ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetIronPoison");
                    return ZNetScene.instance.GetPrefab("ChebGonaz_HelmetBlackIronSkeletonPoison");
                }
                if (leatherArmor) return ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetLeather");
                if (bronzeArmor) return ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetBronze");
                if (ironArmor) return ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetIron");
                return ZNetScene.instance.GetPrefab("ChebGonaz_HelmetBlackIronSkeleton");
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
                    GetHelmetPrefab(),
                    ZNetScene.instance.GetPrefab("ArmorLeatherChest"),
                    ZNetScene.instance.GetPrefab("ArmorLeatherLegs"),
                    ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                if (SkeletonWand.durabilityDamage.Value) { Player.m_localPlayer.GetRightItem().m_durability -= SkeletonWand.durabilityDamageLeather.Value; }
            }
            else if (bronzeArmor)
            {
                defaultItems.AddRange(new GameObject[] {
                    GetHelmetPrefab(),
                    ZNetScene.instance.GetPrefab("ArmorBronzeChest"),
                    ZNetScene.instance.GetPrefab("ArmorBronzeLegs"),
                    ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                if (SkeletonWand.durabilityDamage.Value) { Player.m_localPlayer.GetRightItem().m_durability -= SkeletonWand.durabilityDamageBronze.Value; }
            }
            else if (ironArmor)
            {
                defaultItems.AddRange(new GameObject[] {
                    GetHelmetPrefab(),
                    ZNetScene.instance.GetPrefab("ArmorIronChest"),
                    ZNetScene.instance.GetPrefab("ArmorIronLegs"),
                    ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                if (SkeletonWand.durabilityDamage.Value) { Player.m_localPlayer.GetRightItem().m_durability -= SkeletonWand.durabilityDamageIron.Value; }
            }
            else if (blackIronArmor)
            {
                defaultItems.AddRange(new GameObject[] {
                    GetHelmetPrefab(),
                    ZNetScene.instance.GetPrefab("ChebGonaz_ArmorBlackIronChest"),
                    ZNetScene.instance.GetPrefab("ChebGonaz_ArmorBlackIronLegs"),
                    ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                if (SkeletonWand.durabilityDamage.Value) { Player.m_localPlayer.GetRightItem().m_durability -= SkeletonWand.durabilityDamageBlackIron.Value; }
            }

            humanoid.m_defaultItems = defaultItems.ToArray();

            humanoid.GiveDefaultItems();

            if (SkeletonWand.durabilityDamage.Value)
            {
                switch (skeletonType)
                {
                    case SkeletonType.Archer:
                        Player.m_localPlayer.GetRightItem().m_durability -= SkeletonWand.durabilityDamageArcher.Value;
                        break;
                    case SkeletonType.Mage:
                        Player.m_localPlayer.GetRightItem().m_durability -= SkeletonWand.durabilityDamageMage.Value;
                        break;
                    case SkeletonType.Poison:
                        Player.m_localPlayer.GetRightItem().m_durability -= SkeletonWand.durabilityDamagePoison.Value;
                        break;
                    default:
                        Player.m_localPlayer.GetRightItem().m_durability -= SkeletonWand.durabilityDamageWarrior.Value;
                        break;
                }
            }
        }
    }
}
