using System;
using GameNetcodeStuff;
using HarmonyLib;
using LethalPerformance.Patcher.API;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(PlayerControllerB))]
internal static class Patch_PlayerControllerB
{
    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    /// <summary>
    /// Fixes local username billboard actives and deactives in the same frame
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(PlayerControllerB.ShowNameBillboard))]
    public static bool FixLocalBillBoardIsEnabling(PlayerControllerB __instance, bool __runOriginal)
    {
        if (!__runOriginal)
        {
            return __runOriginal;
        }

        var isLocalPlayer = __instance == StartOfRound.Instance.localPlayerController;
        return !isLocalPlayer;
    }
}
