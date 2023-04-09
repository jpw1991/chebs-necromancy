using System.Collections.Generic;
using ChebsNecromancy.Minions;
using ChebsValheimLibrary.Minions;
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
    [HarmonyPatch(typeof(CharacterDrop), "GenerateDropList")]
    class CharacterDrop_Patches
    {
        [HarmonyPrefix]
        static void AddBonesToDropList(ref List<CharacterDrop.Drop> ___m_drops, CharacterDrop __instance)
        {
            if (BasePlugin.BoneFragmentsDroppedAmountMin.Value >= 0
                && BasePlugin.BoneFragmentsDroppedAmountMax.Value > 0
                // for some stupid reason, the GuardianWraith somehow drops bones even though it shouldn't even have
                // a CharacterDrop component on it. I guess somehow it's being added on. Just ignore it
                && !__instance.TryGetComponent(out GuardianWraithMinion _))
            {
                CharacterDrop.Drop bones = new()
                {
                    m_prefab = ZNetScene.instance.GetPrefab("BoneFragments"),
                    m_onePerPlayer = true,
                    m_amountMin = BasePlugin.BoneFragmentsDroppedAmountMin.Value,
                    m_amountMax = BasePlugin.BoneFragmentsDroppedAmountMax.Value,
                    m_chance = BasePlugin.BoneFragmentsDroppedChance.Value
                };
                ___m_drops.Add(bones);
            }

            // if (__instance.TryGetComponent(out UndeadMinion undeadMinion))
            //     //&& undeadMinion.ItemsDropped)
            // {
            //     Jotunn.Logger.LogInfo($"name={__instance.name}");
            //     if (undeadMinion.ItemsDropped)
            //     {
            //         Jotunn.Logger.LogInfo($"Removing all drops from {__instance.name}");
            //         ___m_drops.RemoveAll(a => true);                    
            //     }
            // }
            
            // Although Container component is on the Neckro, its OnDestroyed
            // isn't called on the death of the creature. So instead, implement
            // its same functionality in the creature's OnDeath instead.
            if (__instance.TryGetComponent(out NeckroGathererMinion _))
            {
                if (__instance.TryGetComponent(out Container container))
                {
                    container.DropAllItems(container.m_destroyedLootPrefab);
                }
            }

            // For all other minions, check if they're supposed to be dropping
            // items and whether these should be packed into a crate or not.
            // We don't want ppls surtling cores and things to be claimed by davey jones
            else if (__instance.TryGetComponent(out UndeadMinion undeadMinion))
            {
                if (undeadMinion is SkeletonMinion
                    && SkeletonMinion.DropOnDeath.Value != ChebGonazMinion.DropType.Nothing
                    && SkeletonMinion.PackDropItemsIntoCargoCrate.Value)
                {
                    undeadMinion.DepositIntoNearbyDeathCrate(__instance);
                    // var depositedSuccessfully = undeadMinion.DepositIntoNearbyDeathCrate(__instance);
                    // undeadMinion.ItemsDropped = depositedSuccessfully;
                    //__instance.m_dropsEnabled = !depositedSuccessfully;
                    //__instance.m_drops.RemoveAll(a => depositedSuccessfully);
                    //Jotunn.Logger.LogInfo($"depositedSuccessfully={depositedSuccessfully}, m_dropsEnabled={__instance.m_dropsEnabled}");
                }
                else if (undeadMinion is DraugrMinion
                    && DraugrMinion.DropOnDeath.Value != ChebGonazMinion.DropType.Nothing
                    && DraugrMinion.PackDropItemsIntoCargoCrate.Value)
                {
                    undeadMinion.DepositIntoNearbyDeathCrate(__instance);
                    // var depositedSuccessfully = undeadMinion.DepositIntoNearbyDeathCrate(__instance);
                    // undeadMinion.ItemsDropped = depositedSuccessfully;
                    //__instance.m_dropsEnabled = !depositedSuccessfully;
                    //__instance.m_drops.RemoveAll(a => depositedSuccessfully);
                    //Jotunn.Logger.LogInfo($"depositedSuccessfully={depositedSuccessfully}, m_dropsEnabled={__instance.m_dropsEnabled}");
                }

                if (undeadMinion.ItemsDropped)
                {
                    Jotunn.Logger.LogInfo("items dropped is true");
                    ___m_drops.RemoveAll(a => true);
                }
            }
        }
    }
    
    // [HarmonyPatch(typeof(CharacterDrop), "OnDeath")]
    // class OnDeathDropPatch
    // {
    //     [HarmonyPrefix]
    //     static void Prefix(CharacterDrop __instance)
    //     {
    //         // Although Container component is on the Neckro, its OnDestroyed
    //         // isn't called on the death of the creature. So instead, implement
    //         // its same functionality in the creature's OnDeath instead.
    //         if (__instance.TryGetComponent(out NeckroGathererMinion _))
    //         {
    //             if (__instance.TryGetComponent(out Container container))
    //             {
    //                 container.DropAllItems(container.m_destroyedLootPrefab);
    //             }
    //         }
    //
    //         // For all other minions, check if they're supposed to be dropping
    //         // items and whether these should be packed into a crate or not.
    //         // We don't want ppls surtling cores and things to be claimed by davey jones
    //         else if (__instance.TryGetComponent(out UndeadMinion undeadMinion))
    //         {
    //             if (undeadMinion is SkeletonMinion
    //                 && SkeletonMinion.DropOnDeath.Value != UndeadMinion.DropType.Nothing
    //                 && SkeletonMinion.PackDropItemsIntoCargoCrate.Value)
    //             {
    //                 undeadMinion.DepositIntoNearbyDeathCrate(__instance);
    //                 // var depositedSuccessfully = undeadMinion.DepositIntoNearbyDeathCrate(__instance);
    //                 // undeadMinion.ItemsDropped = depositedSuccessfully;
    //                 //__instance.m_dropsEnabled = !depositedSuccessfully;
    //                 //__instance.m_drops.RemoveAll(a => depositedSuccessfully);
    //                 //Jotunn.Logger.LogInfo($"depositedSuccessfully={depositedSuccessfully}, m_dropsEnabled={__instance.m_dropsEnabled}");
    //             }
    //             else if (undeadMinion is DraugrMinion
    //                 && DraugrMinion.DropOnDeath.Value != UndeadMinion.DropType.Nothing
    //                 && DraugrMinion.PackDropItemsIntoCargoCrate.Value)
    //             {
    //                 undeadMinion.DepositIntoNearbyDeathCrate(__instance);
    //                 // var depositedSuccessfully = undeadMinion.DepositIntoNearbyDeathCrate(__instance);
    //                 // undeadMinion.ItemsDropped = depositedSuccessfully;
    //                 //__instance.m_dropsEnabled = !depositedSuccessfully;
    //                 //__instance.m_drops.RemoveAll(a => depositedSuccessfully);
    //                 //Jotunn.Logger.LogInfo($"depositedSuccessfully={depositedSuccessfully}, m_dropsEnabled={__instance.m_dropsEnabled}");
    //             }
    //         }
    //     }
    // }
}