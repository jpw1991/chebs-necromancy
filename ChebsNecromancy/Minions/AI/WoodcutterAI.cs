using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChebsNecromancy.Minions.AI
{
    internal class WoodcutterAI : MonoBehaviour
    {
        private float nextCheck;

        private MonsterAI _monsterAI;
        private Humanoid _humanoid;

        private readonly int defaultMask = LayerMask.GetMask("Default");
        
        private static List<Transform> _transforms = new();

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
            if (_monsterAI.GetFollowTarget() != null) return;
            
            // Trees: TreeBase
            // Stumps: Destructible with type Tree
            // Logs: TreeLog
            var closest =
                UndeadMinion.FindClosest<Transform>(transform, SkeletonWoodcutterMinion.LookRadius.Value, defaultMask, 
                    a => !_transforms.Contains(a), false);
            
            // if closest turns up nothing, pick the closest from the claimed transforms list (if there's nothing else
            // to whack, may as well whack a log right next to you, even if another skeleton is already whacking it)
            if (closest == null)
            {
                closest = _transforms
                    .OrderBy(t => Vector3.Distance(t.position, transform.position))
                    .FirstOrDefault();
            }
            
            if (closest != null)
            {
                // prioritize stumps, then logs, then trees
                Destructible destructible = closest.GetComponentInParent<Destructible>();
                if (destructible != null && destructible.GetDestructibleType() == DestructibleType.Tree)
                {
                    _transforms.Add(closest);
                    _monsterAI.SetFollowTarget(destructible.gameObject);
                    _status = "Moving to stump.";
                    return;
                }

                TreeLog treeLog = closest.GetComponentInParent<TreeLog>();
                if (treeLog != null)
                {
                    _transforms.Add(closest);
                    _monsterAI.SetFollowTarget(treeLog.gameObject);
                    _status = "Moving to log.";
                    return;
                }

                TreeBase tree = closest.GetComponentInParent<TreeBase>();
                if (tree != null)
                {
                    _transforms.Add(closest);
                    _monsterAI.SetFollowTarget(tree.gameObject);
                    _status = "Moving to tree.";
                }
            }
        }

        private void Update()
        {
            if (_monsterAI.GetFollowTarget() != null) transform.LookAt(_monsterAI.GetFollowTarget().transform.position + Vector3.down);
            if (Time.time > nextCheck)
            {
                nextCheck = Time.time + SkeletonWoodcutterMinion.UpdateDelay.Value;
                
                LookForCuttableObjects();
                if (_monsterAI.GetFollowTarget() != null
                    && Vector3.Distance(_monsterAI.GetFollowTarget().transform.position, transform.position) < 5)
                {
                    _monsterAI.DoAttack(null, false);
                }
                
                _transforms.RemoveAll(item => item == null);

                if (_monsterAI.GetFollowTarget() == null) _status = "Can't find tree.";

                _humanoid.m_name = _status;
            }
        }
    }
}
