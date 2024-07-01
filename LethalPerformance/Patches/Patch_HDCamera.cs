using System;
using HarmonyLib;
using LethalPerformance.API;
using LethalPerformance.Unity;
using UnityEngine.Rendering.HighDefinition;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(HDCamera))]
internal static unsafe class Patch_HDCamera
{
    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(HDCamera.UpdateShaderVariablesXRCB))]
    public static unsafe bool Prefix(HDCamera __instance, ref ShaderVariablesXR cb)
    {
        fixed (void* p = &cb, views = __instance.m_XRViewConstants)
        {
            Testing.Test((Testing.ReadableViewConstants*)views, __instance.viewCount, (Testing.ReadableShaderVariablesXR*)p);
            return false;
        }
    }
}
