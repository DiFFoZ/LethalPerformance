#if ENABLE_PROFILER
using System;
using HarmonyLib;
using LethalPerformance.API;
using Unity.Profiling;
using UnityEngine.Rendering.HighDefinition;

namespace LethalPerformance.Metrics;

[HarmonyPatch(typeof(HDCamera))]
[IgnoredByDeepProfiler]
[HarmonyPriority(Priority.Last)]
internal static class Patch_HDCameraPerformanceTest
{
    private static readonly ProfilerMarker s_UpdateShaderVariablesXRCB = new("DiFFoZ.HDCamera.UpdateShaderVariablesXRCB");
    private static readonly ProfilerMarker s_UpdateShaderVariablesGlobalCB = new("DiFFoZ.HDCamera.UpdateShaderVariablesGlobalCB");

    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(HDCamera.UpdateShaderVariablesXRCB))]
    public static void UpdateShaderVariablesXRCBBefore()
    {
        s_UpdateShaderVariablesXRCB.Begin();
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(HDCamera.UpdateShaderVariablesXRCB))]
    public static void UpdateShaderVariablesXRCBAfter()
    {
        s_UpdateShaderVariablesXRCB.End();
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(HDCamera.UpdateShaderVariablesGlobalCB),
        [typeof(ShaderVariablesGlobal), typeof(int)], [ArgumentType.Ref, ArgumentType.Normal])]
    public static void UpdateShaderVariablesGlobalCBBefore()
    {
        s_UpdateShaderVariablesGlobalCB.Begin();
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(HDCamera.UpdateShaderVariablesGlobalCB),
        [typeof(ShaderVariablesGlobal), typeof(int)], [ArgumentType.Ref, ArgumentType.Normal])]
    public static void UpdateShaderVariablesGlobalCBAfter()
    {
        s_UpdateShaderVariablesGlobalCB.End();
    }
}
#endif