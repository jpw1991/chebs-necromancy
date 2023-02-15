using System;
using System.Linq;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Minions.AI
{
    internal class WoodcutterAI : MonoBehaviour
    {
        const float LookRadius = 100;

        private const float NextCheckInterval = 5f;
        private float nextCheck;

        private MonsterAI _monsterAI;

        private readonly int defaultMask = LayerMask.GetMask("Default");

        private void Awake()
        {
            _monsterAI = GetComponent<MonsterAI>();
            
        }

        public void LookForCuttableObjects()
        {
            // Trees: TreeBase
            // Stumps: Destructible with type Tree
            // Logs: TreeLog

            Collider[] hitColliders = Physics.OverlapSphere(transform.position + Vector3.up, LookRadius, defaultMask);
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
                    _monsterAI.SetFollowTarget(destructible.gameObject);
                    return;
                }

                TreeLog treeLog = closest.GetComponentInParent<TreeLog>();
                if (treeLog != null)
                {
                    _monsterAI.SetFollowTarget(treeLog.gameObject);
                    return;
                }

                TreeBase tree = closest.GetComponentInParent<TreeBase>();
                if (tree != null)
                {
                    _monsterAI.SetFollowTarget(tree.gameObject);
                    return;
                }
                
                Logger.LogInfo("Nothing found");
            }
        }

        private void Update()
        {
            var followTarget = _monsterAI.GetFollowTarget();
            if (followTarget != null) transform.LookAt(followTarget.transform.position + Vector3.down);
            if (Time.time > nextCheck)
            {
                nextCheck += NextCheckInterval;
                
                LookForCuttableObjects();
                if (followTarget != null
                    && Vector3.Distance(followTarget.transform.position, transform.position) < 5)
                {
                    _monsterAI.DoAttack(null, false);
                }
            }
        }
    }
}
