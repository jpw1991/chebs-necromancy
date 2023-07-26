using ChebsValheimLibrary.Minions.AI;

namespace ChebsNecromancy.Minions.Skeletons.WorkerAI
{
    public class SkeletonMinerAI : MinerAI
    {
        public override float UpdateDelay => SkeletonMinerMinion.UpdateDelay.Value;
        public override float LookRadius => SkeletonMinerMinion.LookRadius.Value;
        public override float RoamRange => UndeadMinion.RoamRange.Value;
        public override string RockInternalIDsList => SkeletonMinerMinion.RockInternalIDsList.Value;
        public override float ToolDamage => SkeletonMinerMinion.ToolDamage.Value;
        public override short ToolTier => SkeletonMinerMinion.ToolTier.Value;
        public override float ChatInterval => SkeletonMinerMinion.ChatInterval.Value;
        public override float ChatDistance => SkeletonMinerMinion.ChatDistance.Value;
    }
}