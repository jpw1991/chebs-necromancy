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
    [HarmonyPatch(typeof(ImpactEffect), "OnCollisionEnter")]
    class ImpactEffectPatch
    {
        // stop minions from damaging ships
        static bool Prefix(ref Collision info, ImpactEffect __instance)
        {
            if (info.gameObject.TryGetComponent(out UndeadMinion _))
            {
                return false; // deny base method completion
            }

            return true;
        }
    }
}