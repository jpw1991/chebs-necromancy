using System;
using System.Linq;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Minions.AI
{
    internal class WoodcutterAI : MonoBehaviour
    {
        private float nextCheck;

        private MonsterAI _monsterAI;
        private Humanoid _humanoid;

        private readonly int defaultMask = LayerMask.GetMask("Default");

        private string _status;

        private void Awake()
        {
            _monsterAI = GetComponent<MonsterAI>();
            _humanoid = GetComponent<Humanoid>();

            _monsterAI.m_alertRange = 1f; // don't attack unless something comes super close - focus on the wood
            _monsterAI.m_randomMoveRange = SkeletonWoodcutterMinion.RoamRange.Value;
        }

        public void LookForCuttableObjects()
        {
            // Trees: TreeBase
            // Stumps: Destructible with type Tree
            // Logs: TreeLog

            Collider[] hitColliders = Physics.OverlapSphere(transform.position + Vector3.up, SkeletonWoodcutterMinion.LookRadius.Value, defaultMask);
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
                    _status = "Moving to stump.";
                    return;
                }

                TreeLog treeLog = closest.GetComponentInParent<TreeLog>();
                if (treeLog != null)
                {
                    _monsterAI.SetFollowTarget(treeLog.gameObject);
                    _status = "Moving to log.";
                    return;
                }

                TreeBase tree = closest.GetComponentInParent<TreeBase>();
                if (tree != null)
                {
                    _monsterAI.SetFollowTarget(tree.gameObject);
                    _status = "Moving to tree.";
                    return;
                }

                _status = "Can't find wood.";
            }
        }

        private void Update()
        {
            var followTarget = _monsterAI.GetFollowTarget();
            if (followTarget != null) transform.LookAt(followTarget.transform.position + Vector3.down);
            if (Time.time > nextCheck)
            {
                nextCheck += SkeletonWoodcutterMinion.UpdateDelay.Value;
                
                LookForCuttableObjects();
                if (followTarget != null
                    && Vector3.Distance(followTarget.transform.position, transform.position) < 5)
                {
                    _monsterAI.DoAttack(null, false);
                }

                _humanoid.m_name = _status;
            }
        }
    }
}
