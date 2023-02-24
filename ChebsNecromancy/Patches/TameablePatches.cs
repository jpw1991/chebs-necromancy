using ChebsNecromancy.Minions;
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
    [HarmonyPatch(typeof(Tameable), "Interact")]
    class TameablePatch1
    {
        [HarmonyPrefix]
        static bool InteractPrefix(Humanoid user, bool hold, bool alt, Tameable __instance)
        {
            // Stop players that aren't the owner of a minion from interacting
            // with it. Also call UndeadMinion wait/follow methods to
            // properly update the ZDO with the waiting position.
            if (__instance.TryGetComponent(out UndeadMinion undeadMinion)
                && user.TryGetComponent(out Player player))
            {
                if (!undeadMinion.BelongsToPlayer(player.GetPlayerName()))
                {
                    user.Message(MessageHud.MessageType.Center, "$chebgonaz_notyourminion");
                    return false; // deny base method completion
                }

                if (!UndeadMinion.Commandable.Value)
                {
                    return false; // deny base method completion
                }

                // use the minion methods to ensure the ZDO is updated
                if (undeadMinion.Status == UndeadMinion.State.Following)
                {
                    user.Message(MessageHud.MessageType.Center, "$friendlyskeletonwand_skeletonwaiting");
                    undeadMinion.Wait(player.transform.position);
                    return false; // deny base method completion
                }
                else
                {
                    user.Message(MessageHud.MessageType.Center, "$friendlyskeletonwand_skeletonfollowing");
                    undeadMinion.Follow(player.gameObject);
                    return false; // deny base method completion
                }
            }

            return true; // permit base method to complete
        }
    }

    [HarmonyPatch(typeof(Tameable))]
    class TameablePatch2
    {
        [HarmonyPatch(nameof(Tameable.GetHoverText))]
        [HarmonyPostfix]
        static void Postfix(Tameable __instance, ref string __result)
        {
            if (__instance.m_nview.IsValid()
                && __instance.m_commandable
                && __instance.TryGetComponent(out UndeadMinion _)
                && __instance.TryGetComponent(out MonsterAI monsterAI)
                && Player.m_localPlayer != null)
            {
                __result = monsterAI.GetFollowTarget() == Player.m_localPlayer.gameObject
                    ? Localization.instance.Localize("$chebgonaz_wait")
                    : Localization.instance.Localize("$chebgonaz_follow");
            }
        }
    }
    
    [HarmonyPatch(typeof(Tameable))]
    class TameablePatch3
    {
        [HarmonyPatch(nameof(Tameable.GetHoverName))]
        [HarmonyPostfix]
        static void Postfix(Tameable __instance, ref string __result)
        {
            if (__instance.m_nview.IsValid()
                && __instance.TryGetComponent(out UndeadMinion undeadMinion))
            {
                __result = $"{Localization.instance.Localize("$chebgonaz_owner")}: {undeadMinion.UndeadMinionMaster} ({undeadMinion.Status})";
            }
        }
    }
}