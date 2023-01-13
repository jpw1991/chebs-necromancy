using UnityEngine;

namespace FriendlySkeletonWand.Minions
{
    internal class BatBeaconBatMinion : UndeadMinion
    {
        private float createdAt;

        private void Awake()
        {
            canBeCommanded = false;
            createdAt = Time.time;
        }

        private void Update()
        {
            if (Time.time > createdAt + BatBeacon.batDuration.Value)
            {
                if (TryGetComponent(out Humanoid humanoid))
                {
                    humanoid.SetHealth(0);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
