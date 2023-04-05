using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ChebsNecromancy.Minions.AI
{
    internal class MinerAI : MonoBehaviour
    {
        private float nextCheck;

        private MonsterAI _monsterAI;
        private Humanoid _humanoid;
        private List<string> _rocksList;

        private string _status;
        
        private bool _inContact;
        private float _lerpedValue;

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
            if (_monsterAI.GetFollowTarget() != null) return;

            // All rocks are in the static_solid layer and have a Destructible component with type Default.
            // We can just match names as the rock names are pretty unique
            var layerMask = 1 << LayerMask.NameToLayer("static_solid") | 1 << LayerMask.NameToLayer("Default_small");
            // get all nearby rocks/ores
            var nearby = UndeadMinion.FindNearby<Transform>(transform,
                SkeletonMinerMinion.LookRadius.Value,
                layerMask,
                Hittable,
                false);
            // assign a priority to the rocks/ores -> eg. copper takes precedence over simple rocks
            var priorities = nearby
                .Select(c => (c, c.name.Contains("_Tin")
                                 || c.name.Contains("silver")
                                 || c.name.Contains("copper")
                    ? 1
                    : 2));
            
            // Order the list of tuples by priority first, then by distance
            var orderedPriorities = priorities.OrderBy(t => t.Item2)
                .ThenBy(t => Vector3.Distance(transform.position, t.Item1.position));

            // Get the first item from the ordered list
            var closest = orderedPriorities.FirstOrDefault().ToTuple()?.Item1;
            if (closest != null)
            {
                _monsterAI.SetFollowTarget(closest.gameObject);
            }
        }

        private void Update()
        {
            var followTarget = _monsterAI.GetFollowTarget();
            if (followTarget != null)
            {
                if (Vector3.Distance(transform.position, followTarget.transform.position) < 5f)
                {
                    transform.LookAt(followTarget.transform.position);// + Vector3.down * _lerpedValue);
                }
                TryAttack();
            }

            if (Time.time > nextCheck)
            {
                nextCheck = Time.time + SkeletonMinerMinion.UpdateDelay.Value
                                      + Random.value; // add a fraction of a second so that multiple
                                                      // workers don't all simultaneously scan
                
                LookForMineableObjects();

                _status = _monsterAI.GetFollowTarget() != null
                    ? $"Moving to rock ({_monsterAI.GetFollowTarget().name})."
                    : "Can't find rocks.";

                _humanoid.m_name = _status;
            }
        }

        private void TryAttack()
        {
            var followTarget = _monsterAI.GetFollowTarget();
            if (followTarget != null && _inContact)
            {
                _monsterAI.DoAttack(null, false);

                var destructible = followTarget.GetComponentInParent<Destructible>();
                if (destructible != null)
                {
                    var hitData = new HitData();
                    hitData.m_damage.m_pickaxe = 500;
                    destructible.m_nview.InvokeRPC("Damage", hitData);
                    return;
                }

                var mineRock5 = followTarget.GetComponentInParent<MineRock5>();
                if (mineRock5 != null)
                {
                    // destroy all fragments
                    for (int i = 0; i < mineRock5.m_hitAreas.Count; i++)
                    {
                        var hitArea = mineRock5.m_hitAreas[i];
                        if (hitArea.m_health > 0f)
                        {
                            var hitData = new HitData();
                            hitData.m_damage.m_damage = hitArea.m_health;
                            hitData.m_point = hitArea.m_collider.bounds.center;
                            hitData.m_toolTier = 100;
                            mineRock5.DamageArea(i, hitData);
                        }
                    }
                    return;
                }

                var mineRock = followTarget.GetComponentInParent<MineRock>();
                if (mineRock != null)
                {
                    // destroy all fragments
                    for (int i = 0; i < mineRock.m_hitAreas.Length; i++)
                    {
                        var col = mineRock.m_hitAreas[i];
                        if (col.TryGetComponent(out HitArea hitArea) && hitArea.m_health > 0f)
                        {
                            var hitData = new HitData();
                            hitData.m_damage.m_damage = hitArea.m_health;
                            hitData.m_point = col.bounds.center;
                            hitData.m_toolTier = 100;
                            mineRock5.DamageArea(i, hitData);
                        }
                    }
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            _inContact = Hittable(collision.gameObject);
        }

        private void OnCollisionExit(Collision other)
        {
            _inContact = Hittable(other.gameObject);
        }

        private bool Hittable(Transform t)
        {
            return Hittable(t.gameObject);
        }
        
        private bool Hittable(GameObject go)
        {
            // Getting miners to hit the right stuff has been a big challenge. This is the closest thing I've been able
            // to come up with. For some reason, checking layers isn't so reliable.
            // History of most of it can be seen here: https://github.com/jpw1991/chebs-necromancy/issues/109
            var destructible = go.GetComponentInParent<Destructible>();
            return _rocksList.FirstOrDefault(rocksListName =>
                   {
                       var parent = go.transform.parent;
                       return parent != null && rocksListName.Contains(parent.name);
                   }) != null
                   || (destructible != null
                       && destructible.m_destructibleType == DestructibleType.Default
                       && destructible.GetComponent<Container>() == null // don't attack containers
                       && destructible.GetComponent<Pickable>() == null // don't attack useful bushes
                    )
                   || go.GetComponentInParent<MineRock5>() != null
                   || go.GetComponentInParent<MineRock>() != null;
        }
    }
}
