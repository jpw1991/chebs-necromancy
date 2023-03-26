using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
            var closest = UndeadMinion.FindClosest<Transform>(transform,
                SkeletonMinerMinion.LookRadius.Value,
                layerMask,
                Hittable,
                false);

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
                    var t = Mathf.PingPong(Time.time, .5f); // This will give you a value between 0 and 1 that oscillates over time.
                    var lerpedValue = Mathf.Lerp(1f, -1f, t); // This will interpolate between 1 and -1 based on the value of t.
                
                    transform.LookAt(followTarget.transform.position + Vector3.down * lerpedValue);                    
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
            if (_monsterAI.GetFollowTarget() != null && _inContact)
            {
                _monsterAI.DoAttack(null, false);
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
                    //&& (destructible.gameObject.layer == LayerMask.NameToLayer("static_solid") || destructible.gameObject.layer == LayerMask.NameToLayer("Default_small"))
                    && destructible.m_destructibleType == DestructibleType.Default
                    && destructible.GetComponent<Container>() == null // don't attack containers
                    && destructible.GetComponent<Pickable>() == null // don't attack useful bushes
                    )
                   || go.GetComponentInParent<MineRock5>() != null
                   || go.GetComponentInParent<MineRock>() != null;
        }
    }
}
