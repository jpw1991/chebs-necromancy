using ChebsNecromancy.Minions;
using HarmonyLib;
using UnityEngine;

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
    [HarmonyPatch(typeof(Aoe), "OnHit")]
    class SharpStakesMinionPatch
    {
        [HarmonyPrefix]
        static bool Prefix(Collider collider, Vector3 hitPoint, Aoe __instance)
        {
            if (collider.TryGetComponent(out UndeadMinion _))
            {
                Piece piece = __instance.GetComponentInParent<Piece>();
                if (piece != null && piece.IsPlacedByPlayer())
                {
                    // stop minion from receiving damage from stakes placed
                    // by a player
                    __instance.m_damage.m_pierce = 0f;
                    // also stop minions from damaging the stakes
                    __instance.m_damageSelf = 0f;
                }
            }

            return true; // permit base method completion
        }
    }
}