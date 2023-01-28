using UnityEngine;

namespace ChebsNecromancy.Minions
{
    internal class SpiritPylonGhostMinion : UndeadMinion
    {
        private float killAt;

        public override void Awake()
        {
            base.Awake();
            canBeCommanded = false;
            killAt = Time.time + SpiritPylon.ghostDuration.Value;
        }

        private void Update()
        {
            if (Time.time > killAt)
            {
                Kill();
            }
        }
    }
}
