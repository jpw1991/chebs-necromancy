using ChebsNecromancy.PvP;
using HarmonyLib;
using Jotunn;

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
    public class PlayerProfilePatches
    {
        [HarmonyPatch(typeof(PlayerProfile))]
        class PlayerProfilePatch1
        {
            [HarmonyPatch(nameof(PlayerProfile.LoadPlayerData))]
            [HarmonyPostfix]
            static void Postfix(Player __instance)
            {
                if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"PlayerProfile.LoadPlayerData postfix - update pvp friends list...");
                PvPManager.UpdatePlayerFriendsDict(BasePlugin.PvPFriendsList.Value);
            }
        }
    }
}