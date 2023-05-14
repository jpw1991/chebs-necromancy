using ChebsNecromancy.Minions;
using ChebsNecromancy.Minions.Skeletons;
using ChebsValheimLibrary.Minions;
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
    [HarmonyPatch(typeof(MonsterAI))]
    class FriendlySkeletonPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(MonsterAI.Awake))]
        static void AwakePostfix(ref Character __instance)
        {
            if (__instance.name.StartsWith("ChebGonaz") && !__instance.TryGetComponent(out ChebGonazMinion _))
            {
                if (__instance.name.Contains("Wraith"))
                {
                    __instance.gameObject.AddComponent<GuardianWraithMinion>();
                }
                else if (__instance.name.Contains("SpiritPylonGhost"))
                {
                    __instance.gameObject.AddComponent<SpiritPylonGhostMinion>();
                }
                else if (__instance.name.Contains("NeckroGatherer"))
                {
                    __instance.gameObject.AddComponent<NeckroGathererMinion>();
                }
                else if (__instance.name.Contains("BattleNeckro"))
                {
                    __instance.gameObject.AddComponent<BattleNeckroMinion>();
                }
                else if (__instance.name.Contains("Bat"))
                {
                    __instance.gameObject.AddComponent<BatBeaconBatMinion>();
                }
                else if (__instance.name.Contains("Leech"))
                {
                    __instance.gameObject.AddComponent<LeechMinion>();
                }
                else if (__instance.name.Contains("Skeleton") || __instance.name.Contains("Draugr"))
                {
                    if (__instance.name.Contains("Miner"))
                    {
                        __instance.gameObject.AddComponent<SkeletonMinerMinion>();
                    }
                        
                    if (__instance.name.Contains("Woodcutter"))
                    {
                        __instance.gameObject.AddComponent<SkeletonWoodcutterMinion>();
                    }

                    if (__instance.name.Contains("PoisonSkeleton"))
                    {
                        __instance.gameObject.AddComponent<PoisonSkeletonMinion>();
                    }
                    else if (__instance.name.Contains("Skeleton"))
                    {
                        __instance.gameObject.AddComponent<SkeletonMinion>();
                    }
                    else if (__instance.name.Contains("Draugr"))
                    {
                        __instance.gameObject.AddComponent<DraugrMinion>();
                    }
                }
            }
        }
    }
}