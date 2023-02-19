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
    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    class CharacterGetDamageModifiersPatch
    {
        // here we basically have to rewrite the entire RPC_Damage verbatim
        // except including the GetBodyArmor that is usually only kept
        // for players and omitted for NPCs. We also discard the durability
        // stuff cuz that doesn't matter for NPCs.
        //
        // I also pruned some player stuff out.
        static bool Prefix(ref long sender, ref HitData hit, Character __instance)
        {
            if (__instance.TryGetComponent(out UndeadMinion minion)
                && (minion is SkeletonMinion || minion is DraugrMinion))
            {
                if (!__instance.m_nview.IsOwner()
                    || __instance.GetHealth() <= 0f
                    || __instance.IsDead()
                    || __instance.IsTeleporting() 
                    || __instance.InCutscene() 
                    || (hit.m_dodgeable && __instance.IsDodgeInvincible()))
                {
                    return false; // deny base method completion
                }
                Character attacker = hit.GetAttacker();
                if (hit.HaveAttacker() && attacker == null)
                {
                    return false; // deny base method completion
                }
                if (attacker != null && !attacker.IsPlayer())
                {
                    float difficultyDamageScalePlayer = Game.instance.GetDifficultyDamageScalePlayer(__instance.transform.position);
                    hit.ApplyModifier(difficultyDamageScalePlayer);
                }
                __instance.m_seman.OnDamaged(hit, attacker);
                if (__instance.m_baseAI !=null 
                    && __instance.m_baseAI.IsAggravatable() 
                    && !__instance.m_baseAI.IsAggravated())
                {
                    BaseAI.AggravateAllInArea(__instance.transform.position, 20f, BaseAI.AggravatedReason.Damage);
                }
                if (__instance.m_baseAI != null 
                    && !__instance.m_baseAI.IsAlerted() 
                    && hit.m_backstabBonus > 1f 
                    && Time.time - __instance.m_backstabTime > 300f)
                {
                    __instance.m_backstabTime = Time.time;
                    hit.ApplyModifier(hit.m_backstabBonus);
                    __instance.m_backstabHitEffects.Create(hit.m_point, Quaternion.identity, __instance.transform);
                }
                if (__instance.IsStaggering() && !__instance.IsPlayer())
                {
                    hit.ApplyModifier(2f);
                    __instance.m_critHitEffects.Create(hit.m_point, Quaternion.identity, __instance.transform);
                }
                if (hit.m_blockable && __instance.IsBlocking())
                {
                    __instance.BlockAttack(hit, attacker);
                }
                __instance.ApplyPushback(hit);
                if (!string.IsNullOrEmpty(hit.m_statusEffect))
                {
                    StatusEffect statusEffect = __instance.m_seman.GetStatusEffect(hit.m_statusEffect);
                    if (statusEffect == null)
                    {
                        statusEffect = __instance.m_seman.AddStatusEffect(
                            hit.m_statusEffect, 
                            false,
                            hit.m_itemLevel, 
                            hit.m_skillLevel);
                    }
                    else
                    {
                        statusEffect.ResetTime();
                        statusEffect.SetLevel(hit.m_itemLevel, hit.m_skillLevel);
                    }
                    if (statusEffect != null && attacker != null)
                    {
                        statusEffect.SetAttacker(attacker);
                    }
                }
                WeakSpot weakSpot = __instance.GetWeakSpot(hit.m_weakSpot);
                if (weakSpot != null)
                {
                    ZLog.Log($"HIT Weakspot: {weakSpot.gameObject.name}");
                }
                HitData.DamageModifiers damageModifiers = __instance.GetDamageModifiers(weakSpot);
                hit.ApplyResistance(damageModifiers, out var significantModifier);
                // // //
                // THIS is what we wrote all the code above for...
                //
                // GetBodyArmor should work, but doesn't. So we tally it up
                // ourselves.
                //
                //float bodyArmor = __instance.GetBodyArmor();
                float bodyArmor = 0f;

                if (__instance.TryGetComponent(out Humanoid humanoid))
                {
                    bodyArmor += humanoid.m_chestItem != null
                        ? humanoid.m_chestItem.m_shared.m_armor : 0;

                    bodyArmor += humanoid.m_legItem != null
                        ? humanoid.m_legItem.m_shared.m_armor : 0;

                    bodyArmor += humanoid.m_helmetItem != null
                        ? humanoid.m_helmetItem.m_shared.m_armor : 0;
                }
                bodyArmor *= SkeletonWand.SkeletonArmorValueMultiplier.Value;
                hit.ApplyArmor(bodyArmor);
                //Jotunn.Logger.LogInfo($"{__instance.name} applied body armor {bodyArmor}");
                // // //
                float poison = hit.m_damage.m_poison;
                float fire = hit.m_damage.m_fire;
                float spirit = hit.m_damage.m_spirit;
                hit.m_damage.m_poison = 0f;
                hit.m_damage.m_fire = 0f;
                hit.m_damage.m_spirit = 0f;
                __instance.ApplyDamage(hit, true, true, significantModifier);
                __instance.AddFireDamage(fire);
                __instance.AddSpiritDamage(spirit);
                __instance.AddPoisonDamage(poison);
                __instance.AddFrostDamage(hit.m_damage.m_frost);
                __instance.AddLightningDamage(hit.m_damage.m_lightning);
                return false; // deny base method completion
            }
            return true; // permit base method to complete
        }
    }
}