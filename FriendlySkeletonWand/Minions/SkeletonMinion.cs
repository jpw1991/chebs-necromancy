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

        public void ScaleStats(float necromancyLevel)
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

        public void ScaleEquipment(float necromancyLevel, bool archer, bool leatherArmor)
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
                Jotunn.Logger.LogInfo("this far");
                humanoid.m_randomArmor = new GameObject[] {
                    ZNetScene.instance.GetPrefab("HelmetLeather"),
                    ZNetScene.instance.GetPrefab("ArmorLeatherChest"),
                    ZNetScene.instance.GetPrefab("ArmorLeatherLegs"),
                    };
                // rotate it correct way (otherwise it's sideways)
                //humanoid.m_visEquipment.UpdateBaseModel(); // no effect
                //humanoid.m_visEquipment.UpdateEquipmentVisuals(); // causes helmet to appear sideways
                //humanoid.m_visEquipment.UpdateVisuals(); // no effect
                //humanoid.SetupEquipment(); // causes helmet to appear, but sideways
                //humanoid.SetupVisEquipment(humanoid.m_visEquipment, false); // causes skirt to appear, but no helmet
                //humanoid.m_visEquipment.m_helmet.Rotate(new Vector3(0, 90f, 0)); // causes helmet to appear sideways
            }
        }
    }
}
