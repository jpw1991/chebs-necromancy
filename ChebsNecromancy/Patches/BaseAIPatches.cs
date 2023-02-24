using ChebsNecromancy.Items;
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
    public class BaseAIPatches
    {
        [HarmonyPatch(typeof(BaseAI))]
        class BaseAIPatch
        {
            [HarmonyPatch(nameof(BaseAI.Follow))]
            [HarmonyPostfix]
            static void Postfix(GameObject go, float dt, BaseAI __instance)
            {
                if (__instance.TryGetComponent(out UndeadMinion undeadMinion))
                {
                    // use our custom implementation with custom follow distance
                    float num = Vector3.Distance(go.transform.position, __instance.transform.position);
                    bool run = num > Wand.RunDistance.Value;
                    var approachRange = 
                        undeadMinion is SkeletonWoodcutterMinion or SkeletonMinerMinion or NeckroGathererMinion
                            ? 0.25f
                            : Wand.FollowDistance.Value;
                    if (num < approachRange)
                    {
                        __instance.StopMoving();
                    }
                    else
                    {
                        __instance.MoveTo(dt, go.transform.position, 0f, run);
                    }
                }
            }
        }
    }
}