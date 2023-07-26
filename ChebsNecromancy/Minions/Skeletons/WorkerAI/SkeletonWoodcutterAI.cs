using ChebsValheimLibrary.Minions.AI;

namespace ChebsNecromancy.Minions.Skeletons.WorkerAI
{
    public class SkeletonWoodcutterAI : WoodcutterAI
    {
        public override float UpdateDelay => SkeletonWoodcutterMinion.UpdateDelay.Value;
        public override float LookRadius => SkeletonWoodcutterMinion.LookRadius.Value;
        public override float RoamRange => UndeadMinion.RoamRange.Value;
        public override float ToolDamage => SkeletonWoodcutterMinion.ToolDamage.Value;
        public override short ToolTier => SkeletonWoodcutterMinion.ToolTier.Value;
        public override float ChatInterval => SkeletonWoodcutterMinion.ChatInterval.Value;
        public override float ChatDistance => SkeletonWoodcutterMinion.ChatDistance.Value;
    }
}