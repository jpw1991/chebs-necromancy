using System.Collections;
using System.Collections.Generic;
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

        private void Awake()
        {
            createdOrderIncrementer++;
            createdOrder = createdOrderIncrementer;

            Tameable tameable = GetComponent<Tameable>();
            if (tameable != null)
            {
                // let the minions generate a little necromancy XP for their master
                tameable.m_levelUpOwnerSkill = SkillManager.Instance.GetSkill(BasePlugin.necromancySkillIdentifier).m_skill;
            }

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
            if (!character.IsOwner()) return;

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
                    skeletonType == SkeletonType.Mage ? ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonMageCirclet") : ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetLeather"),
                    ZNetScene.instance.GetPrefab("ArmorLeatherChest"),
                    ZNetScene.instance.GetPrefab("ArmorLeatherLegs"),
                    ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
            }
            else if (bronzeArmor)
            {
                defaultItems.AddRange(new GameObject[] {
                    skeletonType == SkeletonType.Mage ? ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonMageCirclet") : ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetBronze"),
                    ZNetScene.instance.GetPrefab("ArmorBronzeChest"),
                    ZNetScene.instance.GetPrefab("ArmorBronzeLegs"),
                    ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
            }
            else if (ironArmor)
            {
                defaultItems.AddRange(new GameObject[] {
                    skeletonType == SkeletonType.Mage ? ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonMageCirclet") : ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetIron"),
                    ZNetScene.instance.GetPrefab("ArmorIronChest"),
                    ZNetScene.instance.GetPrefab("ArmorIronLegs"),
                    ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
            }
            else if (blackIronArmor)
            {
                defaultItems.AddRange(new GameObject[] {
                    skeletonType == SkeletonType.Mage ? ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonMageCirclet") : ZNetScene.instance.GetPrefab("ChebGonaz_HelmetBlackIronSkeleton"),
                    ZNetScene.instance.GetPrefab("ChebGonaz_ArmorBlackIronChest"),
                    ZNetScene.instance.GetPrefab("ChebGonaz_ArmorBlackIronLegs"),
                    ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    });
            }

            humanoid.m_defaultItems = defaultItems.ToArray();

            humanoid.GiveDefaultItems();
        }
    }
}
