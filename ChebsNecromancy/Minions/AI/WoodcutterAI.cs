using System.Linq;
using UnityEngine;

namespace ChebsNecromancy.Minions.AI
{
    internal class WoodcutterAI : MonsterAI
    {
        const float LookRadius = 100;

        private const float NextCheckInterval = 5f;
        private float nextCheck;

        public void LookForCuttableObjects()
        {
            // Trees: TreeBase
            // Stumps: Destructible with type Tree
            // Logs: TreeLog

            Collider[] hitColliders = Physics.OverlapSphere(transform.position + Vector3.up, LookRadius);
            if (hitColliders.Length < 1) return;
            // order items from closest to furthest, then take closest one
            Collider closest = hitColliders
                .OrderBy(hitCollider => Vector3.Distance(transform.position, hitCollider.transform.position))
                .FirstOrDefault();
            if (closest != null)
            {
                // prioritize stumps, then logs, then trees
                Destructible destructible = closest.GetComponentInParent<Destructible>();
                if (destructible != null && destructible.GetDestructibleType() == DestructibleType.Tree)
                {
                    SetFollowTarget(destructible.gameObject);
                    return;
                }

                TreeLog treeLog = closest.GetComponentInParent<TreeLog>();
                if (treeLog != null)
                {
                    SetFollowTarget(treeLog.gameObject);
                    return;
                }

                TreeBase tree = closest.GetComponentInParent<TreeBase>();
                if (tree != null)
                {
                    SetFollowTarget(tree.gameObject);
                    return;
                }
            }
        }

        private void Update()
        {
            if (Time.time > nextCheck)
            {
                nextCheck += NextCheckInterval;
                
                LookForCuttableObjects();
                if (GetFollowTarget() != null
                    && Vector3.Distance(GetFollowTarget().transform.position, transform.position) < 1)
                {
                    DoAttack(null, false);
                }
            }
        }
    }
}
