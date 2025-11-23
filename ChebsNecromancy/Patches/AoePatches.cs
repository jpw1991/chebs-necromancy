using System.Reflection.Emit;
using ChebsNecromancy.Minions;
using ChebsValheimLibrary.Minions;
using ChebsValheimLibrary.PvP;
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
    [HarmonyPatch(typeof(Aoe), nameof(Aoe.ShouldHit))]
    class ShouldHitPatch
    {
        [HarmonyPostfix]
        static void Postfix(Collider collider, Aoe __instance, ref bool __result)
        {
            var chebGonazMinion = collider.GetComponentInParent<ChebGonazMinion>();
            if (!chebGonazMinion) return;

            var piece = __instance.GetComponentInParent<Piece>();
            if (piece == null || !piece.IsPlacedByPlayer()) return;

            var friendly = FriendlyToMinion(chebGonazMinion, piece);
            if (!friendly) return;

            // stop minion from receiving damage from stakes placed by an allied player
            __result = false;
        }

        static bool FriendlyToMinion(ChebGonazMinion minion, Piece piece)
        {
            if (!BasePlugin.PvPAllowed.Value) return true;

            var minionMaster = minion.UndeadMinionMaster;
            var pieceMasterId = piece.GetCreator();
            var pieceMaster = Player.s_players.Find(player => player.GetPlayerID() == pieceMasterId)?.GetPlayerName();

            return PvPManager.Friendly(minionMaster, pieceMaster);
        }
    }
}
