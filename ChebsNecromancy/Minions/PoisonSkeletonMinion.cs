
using ChebsNecromancy.Items;
using Jotunn;

namespace ChebsNecromancy.Minions
{
    internal class PoisonSkeletonMinion : SkeletonMinion
    {
        public override void ScaleStats(float necromancyLevel)
        {
            Character character = GetComponent<Character>();
            if (character == null)
            {
                Logger.LogError("ScaleStats: Character component is null!");
                return;
            }
            float health = SkeletonWand.PoisonSkeletonBaseHealth.Value + necromancyLevel * SkeletonWand.SkeletonHealthMultiplier.Value;
            character.SetMaxHealth(health);
            character.SetHealth(health);
        }
    }
}
