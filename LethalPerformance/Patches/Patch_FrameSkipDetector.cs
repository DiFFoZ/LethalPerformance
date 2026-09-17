using System;
using Dissonance;
using HarmonyLib;
using LethalPerformance.Patcher.API;

namespace LethalPerformance.Patches;

[HarmonyPatch(typeof(FrameSkipDetector))]
internal static class Patch_FrameSkipDetector
{
    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyPatch(nameof(FrameSkipDetector.IsFrameSkip))]
    [HarmonyPrefix]
    public static bool DisableCapturePipelineResetOnFrameSkip(ref bool __result)
    {
        // Long frames causing restarting the Dissonance capture pipeline, which calls Microphone.Start
        __result = false;
        return false;
    }
}
