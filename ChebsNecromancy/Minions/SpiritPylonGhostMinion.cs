using ChebsNecromancy.Structures;
using UnityEngine;

namespace ChebsNecromancy.Minions
{
    internal class SpiritPylonGhostMinion : UndeadMinion
    {
        public const string PrefabName = "ChebGonaz_SpiritPylonGhost";
        private float killAt;

        public override void Awake()
        {
            base.Awake();
            canBeCommanded = false;
            killAt = Time.time + SpiritPylon.GhostDuration.Value;
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
