using System;
using System.Collections;
using Jotunn.Managers;
using UnityEngine;
namespace FriendlySkeletonWand.Minions
{
    internal class SkeletonMinion : UndeadMinion
    {
        private void Awake()
        {
            if (SkeletonWand.maxSkeletons.Value > 0)
            {
                SkeletonWand.skeletons.Add(gameObject);
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

        public void ScaleEquipment(float necromancyLevel, bool archer)
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
        }
    }
}
