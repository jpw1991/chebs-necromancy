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
    [HarmonyPatch(typeof(WearNTear), "RPC_Damage")]
    class ArrowImpactPatch
    {
        // stop minions from damaging player structures
        static void Prefix(ref HitData hit, Piece ___m_piece)
        {
            if (hit == null) return;
            Character attacker = hit.GetAttacker();
            if (attacker != null 
                && attacker.TryGetComponent(out UndeadMinion _))
            {
                if (___m_piece.IsPlacedByPlayer())
                {
                    hit.m_damage.m_damage = 0f;
                    hit.m_damage.m_blunt = 0f;
                    hit.m_damage.m_slash = 0f;
                    hit.m_damage.m_pierce = 0f;
                    hit.m_damage.m_chop = 0f;
                    hit.m_damage.m_pickaxe = 0f;
                    hit.m_damage.m_fire = 0f;
                    hit.m_damage.m_frost = 0f;
                    hit.m_damage.m_lightning = 0f;
                    hit.m_damage.m_poison = 0f;
                    hit.m_damage.m_spirit = 0f;
                }
            }
        }
    }
}