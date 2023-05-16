using ChebsNecromancy.Minions;
using ChebsNecromancy.Minions.Draugr;
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
    class MonsterAIPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(MonsterAI.Awake))]
        static void AwakePostfix(ref Character __instance)
        {
            // we use "Contains" because the creature name usually has Clone or something else on the end - so we can't
            // do an "Equals"
            if (!__instance.name.StartsWith("ChebGonaz") || __instance.TryGetComponent(out ChebGonazMinion _)) return;
            
            if (__instance.name.Contains("Wraith"))
            {
                __instance.gameObject.AddComponent<GuardianWraithMinion>();
                return;
            }
            if (__instance.name.Contains("SpiritPylonGhost"))
            {
                __instance.gameObject.AddComponent<SpiritPylonGhostMinion>();
                return;
            }
            if (__instance.name.Contains("NeckroGatherer"))
            {
                __instance.gameObject.AddComponent<NeckroGathererMinion>();
                return;
            }
            if (__instance.name.Contains("BattleNeckro"))
            {
                __instance.gameObject.AddComponent<BattleNeckroMinion>();
                return;
            }
            if (__instance.name.Contains("Bat"))
            {
                __instance.gameObject.AddComponent<BatBeaconBatMinion>();
                return;
            }
            if (__instance.name.Contains("Leech"))
            {
                __instance.gameObject.AddComponent<LeechMinion>();
                return;
            }
            if (__instance.name.Contains("Skeleton"))
            {
                if (__instance.name.Contains("Miner"))
                {
                    __instance.gameObject.AddComponent<SkeletonMinerMinion>();
                    return;
                }
                        
                if (__instance.name.Contains("Woodcutter"))
                {
                    __instance.gameObject.AddComponent<SkeletonWoodcutterMinion>();
                    return;
                }

                if (__instance.name.Contains("PoisonSkeleton"))
                {
                    __instance.gameObject.AddComponent<PoisonSkeletonMinion>();
                    return;
                }
                
                if (__instance.name.Contains("ArcherFire"))
                {
                    __instance.gameObject.AddComponent<SkeletonArcherFireMinion>();
                    return;
                }
                
                if (__instance.name.Contains("ArcherFrost"))
                {
                    __instance.gameObject.AddComponent<SkeletonArcherFrostMinion>();
                    return;
                }
                
                if (__instance.name.Contains("ArcherPoison"))
                {
                    __instance.gameObject.AddComponent<SkeletonArcherPoisonMinion>();
                    return;
                }
                
                if (__instance.name.Contains("ArcherSilver"))
                {
                    __instance.gameObject.AddComponent<SkeletonArcherSilverMinion>();
                    return;
                }
                
                if (__instance.name.Contains("ArcherTier3"))
                {
                    __instance.gameObject.AddComponent<SkeletonArcherTier3Minion>();
                    return;
                }
                
                if (__instance.name.Contains("ArcherTier2"))
                {
                    __instance.gameObject.AddComponent<SkeletonArcherTier2Minion>();
                    return;
                }
                
                if (__instance.name.Contains("Archer"))
                {
                    __instance.gameObject.AddComponent<SkeletonArcherTier1Minion>();
                    return;
                }
                
                __instance.gameObject.AddComponent<SkeletonMinion>();
                return;
            }

            if (__instance.name.Contains("Draugr"))
            {
                if (__instance.name.Contains("ArcherFire"))
                {
                    __instance.gameObject.AddComponent<DraugrArcherFireMinion>();
                    return;
                }
                
                if (__instance.name.Contains("ArcherFrost"))
                {
                    __instance.gameObject.AddComponent<DraugrArcherFrostMinion>();
                    return;
                }
                
                if (__instance.name.Contains("ArcherPoison"))
                {
                    __instance.gameObject.AddComponent<DraugrArcherPoisonMinion>();
                    return;
                }
                
                if (__instance.name.Contains("ArcherSilver"))
                {
                    __instance.gameObject.AddComponent<DraugrArcherSilverMinion>();
                    return;
                }
                
                if (__instance.name.Contains("ArcherTier3"))
                {
                    __instance.gameObject.AddComponent<DraugrArcherTier3Minion>();
                    return;
                }
                
                if (__instance.name.Contains("ArcherTier2"))
                {
                    __instance.gameObject.AddComponent<DraugrArcherTier2Minion>();
                    return;
                }
                
                if (__instance.name.Contains("Archer"))
                {
                    __instance.gameObject.AddComponent<DraugrArcherTier1Minion>();
                    return;
                }
                
                __instance.gameObject.AddComponent<DraugrMinion>();
            }
        }
    }
}