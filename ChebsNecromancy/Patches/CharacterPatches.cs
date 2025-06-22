using ChebsNecromancy.Items.Wands;
using ChebsNecromancy.Minions;
using ChebsNecromancy.Minions.Draugr;
using ChebsNecromancy.Minions.Skeletons;
using ChebsNecromancy.Structures;
using HarmonyLib;
using UnityEngine;
using Logger = Jotunn.Logger;

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
            if (__instance == null) return;
            
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

                if (player == null)
                {
                    Logger.LogError($"player is null");
                    return;
                }

                var incomingDamage = hit?.Clone();
                if (incomingDamage == null)
                {
                    Logger.LogError($"hit is null");
                    return;
                }
                
                incomingDamage.ApplyArmor(player.GetBodyArmor());
                incomingDamage.ApplyResistance(player.m_damageModifiers, out _);

                if (BasePlugin.HeavyLogging.Value)
                {
                    var attackerPrefabId = ZDOMan.instance?.GetZDO(hit.m_attacker)?.GetPrefab();
                    var attackerPrefabName = attackerPrefabId != null
                        ? ZNetScene.instance.GetPrefab(attackerPrefabId.GetHashCode()).name
                        : "unknown";
                    Logger.LogInfo($"player receiving damage from {attackerPrefabName}");
                }

                // check if damage would kill player
                if (player.GetHealth() > incomingDamage.GetTotalDamage())
                {
                    if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"player health {player.GetHealth()} > total hit damage {incomingDamage.GetTotalDamage()}, no need to activate phylactery");
                    return;
                }
                
                if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"player health {player.GetHealth()} < total hit damage {incomingDamage.GetTotalDamage()}, set damage to 0 if fueled phylactery exists or player is teleporting");
                
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
                    Logger.LogInfo($"player is teleporting; ignore damage");
                    hit.m_damage = noDamage;
                    return;
                }
                
                if (Phylactery.HasPhylactery)
                {
                    if (BasePlugin.HeavyLogging.Value) Logger.LogInfo("player has a fueled phylactery, teleport to safety");
                    hit.m_damage = noDamage;
                    player.TeleportTo(Phylactery.PhylacteryLocation + Vector3.forward,
                        Quaternion.identity, true);
                    Phylactery.RequestConsumptionOfFuelForPlayerPhylactery();
                }
            }
        }
    }
}