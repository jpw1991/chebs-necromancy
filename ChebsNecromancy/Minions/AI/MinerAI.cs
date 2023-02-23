using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Minions.AI
{
    internal class MinerAI : MonoBehaviour
    {
        private float nextCheck;

        private MonsterAI _monsterAI;
        private Humanoid _humanoid;
        private List<string> _rocksList;

        private readonly int staticSolidMask = LayerMask.GetMask("static_solid");

        private string _status;

        private void Awake()
        {
            _rocksList = SkeletonMinerMinion.RockInternalIDsList.Value.Split(',').ToList();
            _monsterAI = GetComponent<MonsterAI>();
            _humanoid = GetComponent<Humanoid>();
            _monsterAI.m_alertRange = 1f; // don't attack unless something comes super close - focus on the rocks
            _monsterAI.m_randomMoveRange = SkeletonMinerMinion.RoamRange.Value;
        }

        public void LookForMineableObjects()
        {
            // All rocks are in the static_solid layer and have a Destructible component with type Default.
            // We can just match names as the rock names are pretty unique

            Collider[] hitColliders = Physics.OverlapSphere(transform.position + Vector3.up, SkeletonMinerMinion.LookRadius.Value, staticSolidMask);
            if (hitColliders.Length < 1) return;
            // order items from closest to furthest, then take closest one
            Collider closest = hitColliders
                .Where(hitCollider => _rocksList.Exists(item => hitCollider.name.Contains(item)))
                .OrderBy(hitCollider => Vector3.Distance(transform.position, hitCollider.transform.position))
                .FirstOrDefault();
            if (closest != null)
            {
                _monsterAI.SetFollowTarget(closest.gameObject);
                _status = "Moving to rock.";
                return;
            }
            
            _status = "Can't find rocks.";
        }

        private void Update()
        {
            var followTarget = _monsterAI.GetFollowTarget();
            if (followTarget != null) transform.LookAt(followTarget.transform.position + Vector3.down);
            if (Time.time > nextCheck)
            {
                nextCheck += SkeletonMinerMinion.UpdateDelay.Value;
                
                LookForMineableObjects();
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
