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
            Warrior,
            Archer,
            Mage,
            Poison,
        };

        // for limits checking
        private static int _createdOrderIncrementer;
        public int createdOrder;

        public static ConfigEntry<DropType> DropOnDeath;
        public static ConfigEntry<bool> PackDropItemsIntoCargoCrate;

        public new static void CreateConfigs(BasePlugin plugin)
        {
            DropOnDeath = plugin.ModConfig("SkeletonMinion (Server Synced)", "DropOnDeath",
                DropType.JustResources, new ConfigDescription("Whether a minion refunds anything when it dies.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            PackDropItemsIntoCargoCrate = plugin.ModConfig("SkeletonMinion (Server Synced)", "PackDroppedItemsIntoCargoCrate",
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

        public virtual void ScaleStats(float necromancyLevel)
        {
            Character character = GetComponent<Character>();
            if (character == null)
            {
                Logger.LogError("ScaleStats: Character component is null!");
                return;
            }

            // only scale player's skeletons, not other ppls
            if (!BelongsToPlayer(Player.m_localPlayer.GetPlayerName())) return;

            float health = SkeletonWand.SkeletonBaseHealth.Value + necromancyLevel * SkeletonWand.SkeletonHealthMultiplier.Value;
            character.SetMaxHealth(health);
            character.SetHealth(health);
        }

        public virtual void ScaleEquipment(float necromancyLevel, SkeletonType skeletonType, bool leatherArmor, bool bronzeArmor, bool ironArmor, bool blackIronArmor)
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
                if (SkeletonWand.DurabilityDamage.Value) { Player.m_localPlayer.GetRightItem().m_durability -= SkeletonWand.DurabilityDamageLeather.Value; }
            }
            else if (bronzeArmor)
            {
                defaultItems.AddRange(new GameObject[] {
                    GetHelmetPrefab(),
                    ZNetScene.instance.GetPrefab("ArmorBronzeChest"),
                    ZNetScene.instance.GetPrefab("ArmorBronzeLegs"),
                    ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                if (SkeletonWand.DurabilityDamage.Value) { Player.m_localPlayer.GetRightItem().m_durability -= SkeletonWand.DurabilityDamageBronze.Value; }
            }
            else if (ironArmor)
            {
                defaultItems.AddRange(new GameObject[] {
                    GetHelmetPrefab(),
                    ZNetScene.instance.GetPrefab("ArmorIronChest"),
                    ZNetScene.instance.GetPrefab("ArmorIronLegs"),
                    ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                if (SkeletonWand.DurabilityDamage.Value) { Player.m_localPlayer.GetRightItem().m_durability -= SkeletonWand.DurabilityDamageIron.Value; }
            }
            else if (blackIronArmor)
            {
                defaultItems.AddRange(new GameObject[] {
                    GetHelmetPrefab(),
                    ZNetScene.instance.GetPrefab("ChebGonaz_ArmorBlackIronChest"),
                    ZNetScene.instance.GetPrefab("ChebGonaz_ArmorBlackIronLegs"),
                    ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
                if (SkeletonWand.DurabilityDamage.Value) { Player.m_localPlayer.GetRightItem().m_durability -= SkeletonWand.DurabilityDamageBlackIron.Value; }
            }

            humanoid.m_defaultItems = defaultItems.ToArray();

            humanoid.GiveDefaultItems();

            if (SkeletonWand.DurabilityDamage.Value)
            {
                switch (skeletonType)
                {
                    case SkeletonType.Archer:
                        Player.m_localPlayer.GetRightItem().m_durability -= SkeletonWand.DurabilityDamageArcher.Value;
                        break;
                    case SkeletonType.Mage:
                        Player.m_localPlayer.GetRightItem().m_durability -= SkeletonWand.DurabilityDamageMage.Value;
                        break;
                    case SkeletonType.Poison:
                        Player.m_localPlayer.GetRightItem().m_durability -= SkeletonWand.DurabilityDamagePoison.Value;
                        break;
                    default:
                        Player.m_localPlayer.GetRightItem().m_durability -= SkeletonWand.DurabilityDamageWarrior.Value;
                        break;
                }
            }
        }
    }
}
