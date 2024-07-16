using System;
using GameNetcodeStuff;
using HarmonyLib;
using LethalPerformance.API;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(PlayerControllerB))]
internal static class Patch_PlayerControllerB
{
    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(PlayerControllerB.ShowNameBillboard))]
    public static bool FixLocalBillBoardIsEnabling(PlayerControllerB __instance, bool __runOriginal)
    {
        // todo: remove CanvasScaler and GraphicRaycaster from billboard
        if (!__runOriginal)
        {
            return __runOriginal;
        }

        var isLocalPlayer = __instance == StartOfRound.Instance.localPlayerController;
        return !isLocalPlayer;
    }
}
