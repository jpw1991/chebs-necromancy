
namespace FriendlySkeletonWand.Minions
{
    internal class PoisonSkeletonMinion : SkeletonMinion
    {
        public override void ScaleStats(float necromancyLevel)
        {
            Character character = GetComponent<Character>();
            if (character == null)
            {
                Jotunn.Logger.LogError("ScaleStats: Character component is null!");
                return;
            }
            float health = SkeletonWand.poisonSkeletonBaseHealth.Value + necromancyLevel * SkeletonWand.skeletonHealthMultiplier.Value;
            character.SetMaxHealth(health);
            character.SetHealth(health);
        }

        public override void ScaleEquipment(float necromancyLevel, SkeletonType skeletonType, bool leatherArmor, bool bronzeArmor, bool ironArmor, bool blackIronArmor)
        {
            // do nothing
        }
    }
}
