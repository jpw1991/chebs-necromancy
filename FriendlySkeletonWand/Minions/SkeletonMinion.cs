using System;
using System.Collections;
using Jotunn.Managers;
using UnityEngine;
namespace FriendlySkeletonWand.Minions
{
    internal class SkeletonMinion : UndeadMinion
    {
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
            while (Player.m_localPlayer == null)
            {
                yield return new WaitForSeconds(1);
            }
            ScaleStats(Player.m_localPlayer.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.necromancySkillIdentifier).m_skill));
        }

        public virtual void ScaleStats(float necromancyLevel)
        {
            Character character = GetComponent<Character>();
            if (character == null)
            {
                Jotunn.Logger.LogError("ScaleStats: Character component is null!");
                return;
            }
            float health = SkeletonWand.skeletonBaseHealth.Value + necromancyLevel * SkeletonWand.skeletonHealthMultiplier.Value;
            character.SetMaxHealth(health);
            character.SetHealth(health);
        }

        public virtual void ScaleEquipment(float necromancyLevel, bool archer, bool leatherArmor, bool bronzeArmor, bool ironArmor)
        {
            GameObject weapon = null;
            if (necromancyLevel >= 50)
            {
                weapon = archer
                    ? ZNetScene.instance.GetPrefab("skeleton_bow2")
                    : ZNetScene.instance.GetPrefab("draugr_axe");
            }
            else if (necromancyLevel >= 25)
            {
                weapon = archer
                    ? ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonBow2")
                    : ZNetScene.instance.GetPrefab("skeleton_sword2");
            }
            else
            {
                weapon = archer
                    ? ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonBow")
                    : ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonClub");
            }

            if (weapon == null)
            {
                Jotunn.Logger.LogError("ScaleEquipment: weapon is null!");
                return;
            }

            Humanoid humanoid = GetComponent<Humanoid>();
            if (humanoid == null)
            {
                Jotunn.Logger.LogError("ScaleEquipment: humanoid is null!");
                return;
            }

            humanoid.m_randomWeapon = new GameObject[] { weapon };

            if (leatherArmor)
            {
                humanoid.m_defaultItems = new GameObject[] {
                    ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetLeather"),
                    ZNetScene.instance.GetPrefab("ArmorLeatherChest"),
                    ZNetScene.instance.GetPrefab("ArmorLeatherLegs"),
                    ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    };
            }
            else if (bronzeArmor)
            {
                humanoid.m_defaultItems = new GameObject[] {
                    ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetBronze"),
                    ZNetScene.instance.GetPrefab("ArmorBronzeChest"),
                    ZNetScene.instance.GetPrefab("ArmorBronzeLegs"),
                    ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    };
            }
            else if (ironArmor)
            {
                humanoid.m_defaultItems = new GameObject[] {
                    ZNetScene.instance.GetPrefab("ChebGonaz_SkeletonHelmetIron"),
                    ZNetScene.instance.GetPrefab("ArmorIronChest"),
                    ZNetScene.instance.GetPrefab("ArmorIronLegs"),
                    ZNetScene.instance.GetPrefab("CapeDeerHide"),
                    };
            }
        }
    }
}
