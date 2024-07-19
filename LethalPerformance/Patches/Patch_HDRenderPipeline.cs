using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using LethalPerformance.API;
using UnityEngine.Rendering.HighDefinition;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(HDRenderPipeline))]
internal static class Patch_HDRenderPipeline
{
    [HarmonyPrepare]
    public static bool ShouldPatch()
    {
        return LethalPerformancePlugin.Instance.Config.PatchHDRenderPipeline.Value;
    }

    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyPatch(nameof(HDRenderPipeline.PushCameraGlobalMipBiasPass))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> DisablePushCameraGlobalMipBiasPass(IEnumerable<CodeInstruction> _)
    {
        return [new(OpCodes.Ret)];
    }
}
