using UnityEngine;

namespace FriendlySkeletonWand.Minions
{
    internal class SpiritPylonGhostMinion : UndeadMinion
    {
        private float createdAt;

        private void Awake()
        {
            canBeCommanded = false;
            createdAt = Time.time;
        }

        private void Update()
        {
            if (Time.time > createdAt + SpiritPylon.ghostDuration.Value)
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
