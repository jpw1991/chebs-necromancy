using System.Collections.Generic;
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
    [HarmonyPatch(typeof(CharacterDrop), "GenerateDropList")]
    class CharacterDrop_Patches
    {
        [HarmonyPrefix]
        static void AddBonesToDropList(ref List<CharacterDrop.Drop> ___m_drops)
        {
            if (SkeletonWand.BoneFragmentsDroppedAmountMin.Value >= 0
                && SkeletonWand.BoneFragmentsDroppedAmountMax.Value > 0)
            {
                CharacterDrop.Drop bones = new()
                {
                    m_prefab = ZNetScene.instance.GetPrefab("BoneFragments"),
                    m_onePerPlayer = true,
                    m_amountMin = SkeletonWand.BoneFragmentsDroppedAmountMin.Value,
                    m_amountMax = SkeletonWand.BoneFragmentsDroppedAmountMax.Value,
                    m_chance = 1f
                };
                ___m_drops.Add(bones);
            }
        }
    }
    
    [HarmonyPatch(typeof(CharacterDrop), "OnDeath")]
    class OnDeathDropPatch
    {
        static bool Prefix(CharacterDrop __instance)
        {
            // Although Container component is on the Neckro, its OnDestroyed
            // isn't called on the death of the creature. So instead, implement
            // its same functionality in the creature's OnDeath instead.
            if (__instance.TryGetComponent(out NeckroGathererMinion necroNeck))
            {
                if (__instance.TryGetComponent(out Container container))
                {
                    container.DropAllItems(container.m_destroyedLootPrefab);
                    return false; // deny base method completion
                }
            }

            // For all other minions, check if they're supposed to be dropping
            // items and whether tehse should be packed into a crate or not.
            // We don't want ppls surtling cores and things to be claimed by davey jones
            else if (__instance.TryGetComponent(out UndeadMinion undeadMinion))
            {
                void PackDropsIntoCrate()
                {
                    // use vanilla cargo crate -> same as a karve/longboat drops
                    GameObject cratePrefab = ZNetScene.instance.GetPrefab("CargoCrate");
                    if (cratePrefab != null)
                    {
                        // warning: we mustn't ever exceed the maximum storage capacity
                        // of the crate. Not a problem right now, but could be in the future
                        // if the ingredients exceed 4. Right now, can only be 3, so it's fine.
                        // eg. bones, meat, ingot (draugr) OR bones, ingot, surtling core (skele)
                        Inventory inv =
                            GameObject.Instantiate(cratePrefab, __instance.transform.position + Vector3.up, Quaternion.identity)
                            .GetComponent<Container>()
                            .GetInventory();
                        __instance.m_drops.ForEach(drop => inv.AddItem(drop.m_prefab, drop.m_amountMax));
                    }
                }

                if (undeadMinion is SkeletonMinion
                    && SkeletonMinion.DropOnDeath.Value != UndeadMinion.DropType.Nothing
                    && SkeletonMinion.PackDropItemsIntoCargoCrate.Value)
                {
                    PackDropsIntoCrate();
                    return false; // deny base method completion
                }
                if (undeadMinion is DraugrMinion
                    && DraugrMinion.DropOnDeath.Value != UndeadMinion.DropType.Nothing
                    && DraugrMinion.PackDropItemsIntoCargoCrate.Value)
                {
                    PackDropsIntoCrate();
                    return false; // deny base method completion
                }
            }

            return true; // permit base method to complete
        }
    }
}