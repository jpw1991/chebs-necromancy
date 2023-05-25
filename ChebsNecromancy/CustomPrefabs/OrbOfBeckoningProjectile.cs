using System.Collections.Generic;
using ChebsNecromancy.Items.Wands;
using ChebsNecromancy.Minions;
using UnityEngine;

namespace ChebsNecromancy.CustomPrefabs
{
    public class OrbOfBeckoningProjectile : MonoBehaviour
    {
        public Player player;

        private List<MonsterAI> allMinions = new();

        private void Start()
        {
            if (!TryGetComponent(out Projectile projectile)
                || !projectile.m_owner.TryGetComponent(out player))
            {
                // In this case, the item is likely being wielded by an NPC. So return prematurely and don't worry
                // about the code.
                return;
            }

            // make all minions that belong to the player follow the ball
            List<Character> allCharacters = new();
            Character.GetCharactersInRange(player.transform.position, SkeletonWand.SkeletonSetFollowRange.Value, allCharacters);
            foreach (var character in allCharacters)
            {
                if (character.IsDead()) continue;
                
                var minion = character.GetComponent<UndeadMinion>();
                if (minion == null || !minion.canBeCommanded
                                   || !minion.BelongsToPlayer(player.GetPlayerName())) continue;

                if (minion.TryGetComponent(out MonsterAI monsterAI)
                    && monsterAI.GetFollowTarget() == player.gameObject)
                {
                    allMinions.Add(monsterAI);
                    monsterAI.SetFollowTarget(gameObject);
                }
            }
        }

        private void OnDestroy()
        {
            // return control to player
            if (!player) return;
            allMinions.FindAll(monsterAI => monsterAI && monsterAI.GetFollowTarget() == gameObject)
                .ForEach(monsterAI => monsterAI.SetFollowTarget(player.gameObject));
        }
    }
}