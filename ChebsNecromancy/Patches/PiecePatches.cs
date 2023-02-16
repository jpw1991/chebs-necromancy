using ChebsNecromancy.Structures;
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
    [HarmonyPatch(typeof(Piece))]
    class ChebGonaz_PiecePatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Piece.Awake))]
        static void AwakePostfix(ref Piece __instance)
        {
            if (__instance.name.StartsWith("ChebGonaz"))
            {
                if (__instance.name.Contains("SpiritPylon"))
                {
                    if (__instance.GetComponent<SpiritPylon>() == null)
                    {
                        __instance.gameObject.AddComponent<SpiritPylon>();
                    }
                }
                else if (__instance.name.Contains("RefuelerPylon"))
                {
                    if (__instance.GetComponent<RefuelerPylon>() == null)
                    {
                        __instance.gameObject.AddComponent<RefuelerPylon>();
                    }
                }
                else if (__instance.name.Contains("NeckroGathererPylon"))
                {
                    if (__instance.GetComponent<NeckroGathererPylon>() == null)
                    {
                        __instance.gameObject.AddComponent<NeckroGathererPylon>();
                    }
                }
                else if (__instance.name.Contains("BatBeacon"))
                {
                    if (__instance.GetComponent<BatBeacon>() == null)
                    {
                        __instance.gameObject.AddComponent<BatBeacon>();
                    }
                }
                else if (__instance.name.Contains("BatLantern"))
                {
                    if (__instance.GetComponent<BatLantern>() == null)
                    {
                        __instance.gameObject.AddComponent<BatLantern>();
                    }
                }
            }
        }
    }
}