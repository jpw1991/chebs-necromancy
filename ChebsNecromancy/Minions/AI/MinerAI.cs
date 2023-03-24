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
        
        private static List<Transform> _transforms = new();

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
            
            _transforms.RemoveAll(a => a == null); // clean trash
            
            // All rocks are in the static_solid layer and have a Destructible component with type Default.
            // We can just match names as the rock names are pretty unique
            LayerMask layerMask = 1 << LayerMask.NameToLayer("static_solid") | 1 << LayerMask.NameToLayer("Default_small");
            var closest = UndeadMinion.FindClosest<Transform>(transform,
                SkeletonMinerMinion.LookRadius.Value,
                layerMask,
                hitCollider => _rocksList.Exists(item => hitCollider.name.Contains(item)
                                                         && !_transforms.Contains(hitCollider)),
                false);
            
            // if closest turns up nothing, pick the closest from the claimed transforms list (if there's nothing else
            // to whack, may as well whack a rock right next to you, even if another skeleton is already whacking it)
            if (closest == null)
            {
                closest = _transforms
                    .OrderBy(t => Vector3.Distance(t.position, transform.position))
                    .FirstOrDefault();
            }
            
            if (closest != null)
            {
                _transforms.Add(closest);
                _monsterAI.SetFollowTarget(closest.gameObject);
            }
        }

        private void Update()
        {
            if (_monsterAI.GetFollowTarget() != null)
            {
                var t = Mathf.PingPong(Time.time, .5f); // This will give you a value between 0 and 1 that oscillates over time.
                var lerpedValue = Mathf.Lerp(1f, -1f, t); // This will interpolate between 1 and -1 based on the value of t.
                
                transform.LookAt(_monsterAI.GetFollowTarget().transform.position + Vector3.down * lerpedValue);
                TryAttack();
            }
            if (Time.time > nextCheck)
            {
                nextCheck = Time.time + SkeletonMinerMinion.UpdateDelay.Value
                                      + Random.value; // add a fraction of a second so that multiple
                                                      // workers don't all simultaneously scan
                
                LookForMineableObjects();
                // if (_monsterAI.GetFollowTarget() != null
                //     && Vector3.Distance(_monsterAI.GetFollowTarget().transform.position, transform.position) < 5)
                // {
                //     _monsterAI.DoAttack(null, false);
                // }

                _status = _monsterAI.GetFollowTarget() != null
                    ? $"Moving to rock ({_monsterAI.GetFollowTarget().name})."
                    : "Can't find rocks.";

                _humanoid.m_name = _status;
            }
        }

        private void TryAttack()
        {
            if (_monsterAI.GetFollowTarget() != null
                && Vector3.Distance(_monsterAI.GetFollowTarget().transform.position, transform.position) < 2f)
            {
                _monsterAI.DoAttack(null, false);
            }
        }
    }
}
