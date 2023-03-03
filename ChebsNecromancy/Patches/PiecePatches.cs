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
                    if (!__instance.TryGetComponent(out SpiritPylon _))
                    {
                        __instance.gameObject.AddComponent<SpiritPylon>();
                    }
                }
                else if (__instance.name.Contains("RefuelerPylon"))
                {
                    if (!__instance.TryGetComponent(out RefuelerPylon _))
                    {
                        __instance.gameObject.AddComponent<RefuelerPylon>();
                    }
                }
                else if (__instance.name.Contains("NeckroGathererPylon"))
                {
                    if (!__instance.TryGetComponent(out NeckroGathererPylon _))
                    {
                        __instance.gameObject.AddComponent<NeckroGathererPylon>();
                    }
                }
                else if (__instance.name.Contains("BatBeacon"))
                {
                    if (!__instance.TryGetComponent(out BatBeacon _))
                    {
                        __instance.gameObject.AddComponent<BatBeacon>();
                    }
                }
                else if (__instance.name.Contains("BatLantern"))
                {
                    if (!__instance.TryGetComponent(out BatLantern _))
                    {
                        __instance.gameObject.AddComponent<BatLantern>();
                    }
                }
                else if (__instance.name.Contains("FarmingPylon"))
                {
                    if (!__instance.TryGetComponent(out FarmingPylon _))
                    {
                        __instance.gameObject.AddComponent<FarmingPylon>();
                    }
                }
                else if (__instance.name.Contains("RepairPylon"))
                {
                    if (!__instance.TryGetComponent(out RepairPylon _))
                    {
                        __instance.gameObject.AddComponent<RepairPylon>();
                    }
                }
            }
        }
    }
}