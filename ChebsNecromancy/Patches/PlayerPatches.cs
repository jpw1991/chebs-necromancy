using System.Collections.Generic;
using System.Linq;
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
    public class PlayerPatches
    {
        [HarmonyPatch(typeof(Player))]
        class PlayerPatch1
        {
            [HarmonyPatch(nameof(Player.PlayerAttackInput))]
            [HarmonyPostfix]
            static void Postfix(float dt, Player __instance)
            {
                // if attacking with a wand, destroy the minion if you own it
                if (!__instance.m_attack) return;
                var friendlySkeletonWands = new List<string> { "$item_friendlyskeletonwand", "$item_friendlyskeletonwand_draugrwand" };
                var undeadMinion = Physics.OverlapSphere(__instance.transform.position, 2)
                    .Select(collider => collider.GetComponentInParent<UndeadMinion>())
                    .Where(undead => undead != null)
                    .Where(undead => friendlySkeletonWands.Contains(__instance.GetCurrentWeapon()?.m_shared?.m_name))
                    .Where(undead => undead.BelongsToPlayer(__instance.GetPlayerName()))
                    .OrderBy(undead => Vector3.Distance(undead.transform.position, __instance.transform.position))
                    .FirstOrDefault();

                if (undeadMinion) undeadMinion.Kill();
            }
        }
    }
}