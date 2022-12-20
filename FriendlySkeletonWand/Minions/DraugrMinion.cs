using System;
using System.Collections;
using Jotunn.Managers;
using UnityEngine;
namespace FriendlySkeletonWand.Minions
{
    internal class DraugrMinion : UndeadMinion
    {
        private void Awake()
        {
            if (DraugrWand.maxDraugr.Value > 0)
            {
                DraugrWand.draugr.Add(gameObject);
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
            float health = DraugrWand.draugrBaseHealth.Value + necromancyLevel * DraugrWand.draugrHealthMultiplier.Value;
            character.SetMaxHealth(health);
            character.SetHealth(health);
        }
    }
}
