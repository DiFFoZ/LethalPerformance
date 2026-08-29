using System;
using HarmonyLib;
using LethalPerformance.Patcher.API;
using UnityEngine.Rendering.HighDefinition;

namespace LethalPerformance.Patches;

[HarmonyPatch(typeof(HDRenderPipeline))]
internal static class Patch_HDRenderPipeline
{
    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(HDRenderPipeline.ApplyCameraMipBias))]
    [HarmonyPatch(nameof(HDRenderPipeline.ResetCameraMipBias))]
    public static bool SkipApplyCameraMipBias(HDCamera hdCamera)
    {
        return hdCamera.globalMipBias != 0f;
    }
}
