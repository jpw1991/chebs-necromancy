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

#pragma warning disable IDE0051 // Remove unused private members
        private void Update()
#pragma warning restore IDE0051 // Remove unused private members
        {
            if (Time.time > killAt)
            {
                Kill();
            }
        }
    }
}
