using ChebsNecromancy.Items;
using ChebsNecromancy.Items.Wands;
using ChebsNecromancy.Minions;
using ChebsNecromancy.Minions.Skeletons;
using ChebsNecromancy.PvP;
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
    public class BaseAIPatches
    {
        [HarmonyPatch(typeof(BaseAI))]
        class BaseAIPatch
        {
            [HarmonyPatch(nameof(BaseAI.Follow))]
            [HarmonyPostfix]
            static void Postfix(GameObject go, float dt, BaseAI __instance)
            {
                if (__instance.TryGetComponent(out UndeadMinion undeadMinion))
                {
                    // use our custom implementation with custom follow distance
                    float num = Vector3.Distance(go.transform.position, __instance.transform.position);
                    bool run = num > Wand.RunDistance.Value;
                    var approachRange = 
                        undeadMinion is SkeletonWoodcutterMinion or SkeletonMinerMinion or NeckroGathererMinion
                            ? 0.25f
                            : Wand.FollowDistance.Value;
                    if (num < approachRange)
                    {
                        __instance.StopMoving();
                    }
                    else
                    {
                        __instance.MoveTo(dt, go.transform.position, 0f, run);
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(BaseAI))]
        class BaseAIPatch2
        {
            [HarmonyPatch(nameof(BaseAI.IsEnemy), new []{typeof(Character), typeof(Character)})]
            [HarmonyPostfix]
            static void Postfix(Character a, Character b, ref bool __result)
            {
                if (a == null || b == null) return;
                
                // we're checking for PvP here
                if (!BasePlugin.PvPAllowed.Value) return;
                
                var faction1 = a.GetFaction();
                var faction2 = b.GetFaction();
                
                // only act if both things belong to the player faction because all minions and players belong
                // to the player faction and we only care about PvP here
                if (faction1 != Character.Faction.Players || faction2 != Character.Faction.Players) return;
                
                var minionA = a.GetComponent<ChebGonazMinion>();
                var minionB = b.GetComponent<ChebGonazMinion>();

                // var pvpFriendsList = BasePlugin.PvPFriendsList.Value;
                var minionMasterA = minionA != null ? minionA.UndeadMinionMaster : null;
                var minionMasterB = minionB != null ? minionB.UndeadMinionMaster : null;

                if (minionA != null && minionB != null)
                {
                    // pvp between two minions
                    var isUnclaimedMercenary = minionMasterA == "";
                    var targetingUnclaimedMercenary = minionMasterB == "";
                    if (isUnclaimedMercenary || targetingUnclaimedMercenary)
                    {
                        __result = false;
                        return;
                    }

                    if (minionMasterA != minionMasterB)
                    {
                        var minionAFriendlyToMinionB = PvPManager.Friendly(minionMasterA, minionMasterB);
                        var minionBFriendlyToMinionA = PvPManager.Friendly(minionMasterB, minionMasterA);
                        __result = !(minionAFriendlyToMinionB && minionBFriendlyToMinionA);
                    }
                }
                else if (minionB != null)
                {
                    if (a.TryGetComponent(out Player player)
                        && minionMasterB != "" // do nothing if unclaimed minion
                        && minionMasterB != player.GetPlayerName())
                    {
                        var friendly = PvPManager.Friendly(player.GetPlayerName(), minionMasterB);
                        //if (BasePlugin.HeavyLogging.Value) Jotunn.Logger.LogInfo($"Friendly = {friendly}");
                        __result = !friendly;
                    }
                    // B is a player owned thing of some kind
                    // for now, defer to default handling
                }
                else if (minionA != null)
                {
                    if (b.TryGetComponent(out Player player)
                        && minionMasterA != "" // do nothing if unclaimed minion
                        && minionMasterA != player.GetPlayerName())
                    {
                        var friendly = PvPManager.Friendly(player.GetPlayerName(), minionMasterA);
                        //if (BasePlugin.HeavyLogging.Value) Jotunn.Logger.LogInfo($"Friendly = {friendly}");
                        __result = !friendly;
                    }
                    // A is a player owned thing of some kind
                    // for now, defer to default handling
                }
            }
        }
    }
}