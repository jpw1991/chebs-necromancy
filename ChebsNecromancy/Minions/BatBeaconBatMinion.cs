using ChebsNecromancy.Structures;
using UnityEngine;

namespace ChebsNecromancy.Minions
{
    internal class BatBeaconBatMinion : UndeadMinion
    {
        private float killAt;

        public override void Awake()
        {
            base.Awake();
            canBeCommanded = false;
            killAt = Time.time + BatBeacon.BatDuration.Value;
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
