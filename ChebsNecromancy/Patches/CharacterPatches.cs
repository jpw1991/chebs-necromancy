using ChebsNecromancy.Items;
using ChebsNecromancy.Minions;
using HarmonyLib;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedParameter.Local

// Harmony patching is very sensitive regarding parameter names. Everything in this region should be hand crafted
// and not touched by well-meaning but clueless IDE optimizations.
// eg.
// * __instance MUST be named with exactly two underscores.
// * ___m_drops MUST be named with exactly three underscores.
// * Unused parameters must be left there because they must match the method to override
// * All patch methods need to be static
//
// This is because all of this has a special meaning to Harmony.

namespace ChebsNecromancy.Patches
{
    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    class CharacterGetDamageModifiersPatch
    {
        [HarmonyPrefix]
        static void Prefix(ref long sender, ref HitData hit, Character __instance)
        {
            if (__instance.TryGetComponent(out UndeadMinion minion)
                && minion is SkeletonMinion or DraugrMinion)
            {
                // GetBodyArmor should work, but doesn't. So we tally it up
                // ourselves.
                //
                //float bodyArmor = __instance.GetBodyArmor();
                if (__instance.TryGetComponent(out Humanoid humanoid))
                {
                    float bodyArmor = 0f;
                    bodyArmor += humanoid.m_chestItem != null
                        ? humanoid.m_chestItem.m_shared.m_armor : 0;

                    bodyArmor += humanoid.m_legItem != null
                        ? humanoid.m_legItem.m_shared.m_armor : 0;

                    bodyArmor += humanoid.m_helmetItem != null
                        ? humanoid.m_helmetItem.m_shared.m_armor : 0;
                    
                    bodyArmor *= SkeletonWand.SkeletonArmorValueMultiplier.Value;
                    hit.ApplyArmor(bodyArmor);
                }
            }
        }
    }
}
