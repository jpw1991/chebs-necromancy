namespace ChebsNecromancy.Minions
{
    internal class SkeletonWoodcutter : UndeadMinion
    {
        public override void Awake()
        {
            base.Awake();
            
            canBeCommanded = false;
        }
    }
}