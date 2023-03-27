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
        private float _lerpedValue;
        //private float _timeFollowingObject;

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
                //_timeFollowingObject = Time.time;
            }
        }

        private void Update()
        {
            var followTarget = _monsterAI.GetFollowTarget();
            if (followTarget != null)
            {
                if (Vector3.Distance(transform.position, followTarget.transform.position) < 5f)
                {
                    //var t = Mathf.PingPong(Time.time, 2f);
                    //_lerpedValue = Mathf.Lerp(1f, -1f, t);  

                    transform.LookAt(followTarget.transform.position);// + Vector3.down * _lerpedValue);
                    
                }
                //followTarget.GetComponent<Destructible>().Damage(new);
                
                TryAttack();
            }
            // else
            // {
            //     _timeFollowingObject = 0f;
            // }
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
                //&& (_inContact // in contact with rock (physically touching it)
                //|| _timeFollowingObject > 20f // or can't reach it after prolonged period (large rock fragments)
                //))
            {
                _monsterAI.DoAttack(null, false);

                var destructible = followTarget.GetComponentInParent<Destructible>();
                if (destructible != null)
                {
                    var hitData = new HitData();
                    hitData.m_damage.m_pickaxe = 500;
                    destructible.m_nview.InvokeRPC("Damage", hitData);
                    //destructible.Damage(hitData);
                    return;
                }

                var mineRock5 = followTarget.GetComponentInParent<MineRock5>();
                if (mineRock5 != null)
                {
                    // destroy all fragments
                    for (int i = 0; i < mineRock5.m_hitAreas.Count; i++)
                    {
                        var hitArea = mineRock5.m_hitAreas[i];
                        if (hitArea.m_health > 0f)// && !hitArea.m_supported)
                        {
                            var hitData = new HitData();
                            hitData.m_damage.m_damage = hitArea.m_health;
                            hitData.m_point = hitArea.m_collider.bounds.center;
                            hitData.m_toolTier = 100;
                            mineRock5.DamageArea(i, hitData);
                        }
                    }
                    //mineRock5.Damage(hitData);
                    //mineRock5.m_nview.Destroy();
                    return;
                }

                var mineRock = followTarget.GetComponentInParent<MineRock>();
                if (mineRock != null)
                {
                    // destroy all fragments
                    for (int i = 0; i < mineRock.m_hitAreas.Length; i++)
                    {
                        var collider = mineRock.m_hitAreas[i];
                        if (collider.TryGetComponent(out HitArea hitArea) && hitArea.m_health > 0f)
                        {
                            var hitData = new HitData();
                            hitData.m_damage.m_damage = hitArea.m_health;
                            hitData.m_point = collider.bounds.center;
                            hitData.m_toolTier = 100;
                            mineRock5.DamageArea(i, hitData);
                        }
                    }
                    //mineRock.Damage(hitData);
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
