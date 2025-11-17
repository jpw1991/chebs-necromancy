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
    [HarmonyPatch(typeof(Aoe), nameof(Aoe.OnHit))]
    class OnHitPatch
    {
        [HarmonyPrefix]
        static bool Prefix(Collider collider, Vector3 hitPoint, Aoe __instance)
        {
            var undeadMinion = collider.GetComponentInParent<UndeadMinion>();
            if (!undeadMinion) return true; // permit base method completion

            var piece = __instance.GetComponentInParent<Piece>();
            if (piece == null || !piece.IsPlacedByPlayer()) return true; // permit base method completion

            var friendly = FriendlyToMinion(undeadMinion, piece);
            if (!friendly) return true; // permit base method completion

            // stop minion from receiving damage from stakes placed by an allied player
            return false; // deny base method completion
        }

        static bool FriendlyToMinion(UndeadMinion minion, Piece piece)
        {
            Jotunn.Logger.LogInfo($"FriendlyToMinion {minion} {piece}");
            if (!BasePlugin.PvPAllowed.Value) return true;

            var minionMaster = minion.UndeadMinionMaster;
            var pieceMasterId = piece.GetCreator();
            var pieceMaster = Player.s_players.Find(player => player.GetPlayerID() == pieceMasterId)?.GetPlayerName();

            return PvPManager.Friendly(minionMaster, pieceMaster);
        }
    }
}
