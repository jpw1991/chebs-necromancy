using ChebsNecromancy.Items;
using ChebsNecromancy.Items.Wands;
using ChebsNecromancy.Minions;
using ChebsNecromancy.Minions.Draugr;
using ChebsNecromancy.Minions.Skeletons;
using ChebsNecromancy.Structures;
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
        [HarmonyPrefix]
        static void Prefix(ref long sender, ref HitData hit, Character __instance)
        {
            if (__instance.TryGetComponent(out UndeadMinion minion)
                && minion is SkeletonMinion or DraugrMinion)
            {
                // GetBodyArmor should work, but doesn't. So we tally it up
                // ourselves.
                //
                //float bodyArmor = __instance.GetBodyArmor();
                if (__instance.TryGetComponent(out Humanoid humanoid))
                {
                    float bodyArmor = 0f;
                    bodyArmor += humanoid.m_chestItem != null
                        ? humanoid.m_chestItem.m_shared.m_armor
                        : 0;

                    bodyArmor += humanoid.m_legItem != null
                        ? humanoid.m_legItem.m_shared.m_armor
                        : 0;

                    bodyArmor += humanoid.m_helmetItem != null
                        ? humanoid.m_helmetItem.m_shared.m_armor
                        : 0;

                    bodyArmor *= SkeletonWand.SkeletonArmorValueMultiplier.Value;
                    hit.ApplyArmor(bodyArmor);
                }
            }

            if (__instance.IsPlayer())
            {
                var player = (Player)__instance;

                var incomingDamage = hit.Clone();
                incomingDamage.ApplyArmor(player.GetBodyArmor());
                incomingDamage.ApplyResistance(player.m_damageModifiers, out _);

                // check if damage would kill player
                //Jotunn.Logger.LogInfo($"player health {player.GetHealth()}\ntotal hit damage {incomingDamage.GetTotalDamage()}");
                if (player.GetHealth() > incomingDamage.GetTotalDamage())
                {
                    //Jotunn.Logger.LogInfo($"player health {player.GetHealth()} > total hit damage {incomingDamage.GetTotalDamage()}");
                    return;
                }
                
                var noDamage = new HitData.DamageTypes
                {
                    m_damage = 0,
                    m_blunt = 0,
                    m_slash = 0,
                    m_pierce = 0,
                    m_chop = 0,
                    m_pickaxe = 0,
                    m_fire = 0,
                    m_frost = 0,
                    m_lightning = 0,
                    m_poison = 0,
                    m_spirit = 0,
                };

                if (player.IsTeleporting())
                {
                    Jotunn.Logger.LogInfo($"player is teleporting; ignore damage");
                    hit.m_damage = noDamage;
                    return;
                }
                
                var playerPhylactery = Phylactery.Phylacteries.Find(phylactery =>
                    phylactery.TryGetComponent(out Piece piece)
                    && piece.m_creator == player.GetPlayerID());
                if (playerPhylactery != null && playerPhylactery.ConsumeFuel())
                {
                    hit.m_damage = noDamage;
                    player.TeleportTo(playerPhylactery.transform.position + Vector3.forward,
                        Quaternion.identity, true);
                }
            }
        }
    }
}