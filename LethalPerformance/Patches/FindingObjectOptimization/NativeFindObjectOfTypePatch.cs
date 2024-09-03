using System;
using System.Diagnostics;
using HarmonyLib;
using LethalPerformance.Patcher.API;
using LethalPerformance.Utilities;
using UnityEngine;
using UnityEngine.Profiling;

namespace LethalPerformance.Patches.FindingObjectOptimization;

[HarmonyPatch(typeof(Object))]
internal static class NativeFindObjectOfTypePatch
{
    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyPatch(nameof(Object.FindAnyObjectByType), [typeof(Type), typeof(FindObjectsInactive)])]
    [HarmonyPatch(nameof(Object.FindFirstObjectByType), [typeof(Type), typeof(FindObjectsInactive)])]
    [HarmonyPrefix]
    public static bool FindObjectFast(Type type, FindObjectsInactive findObjectsInactive, ref Object? __result)
    {
        ShowInProfilerType(type);
        return !TryFindObjectFast(type, findObjectsInactive, out __result);
    }

    [HarmonyPatch(nameof(Object.FindObjectOfType), [typeof(Type), typeof(bool)])]
    [HarmonyPrefix]
    public static bool FindObjectFast(Type type, bool includeInactive, ref Object? __result)
    {
        ShowInProfilerType(type);
        return !TryFindObjectFast(type, includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            out __result);
    }

    [Conditional("ENABLE_PROFILER")]
    private static void ShowInProfilerType(Type type)
    {
        Profiler.BeginSample("DiFFoZ.Find." + type.Name);
        Profiler.EndSample();
    }

    public static bool TryFindObjectFast(Type type, FindObjectsInactive findObjectsInactive, out Object? __result)
    {
        if (UnsafeCacheManager.TryGetCachedReference(type, findObjectsInactive, out var cache))
        {
            __result = cache;
            return true;
        }

        __result = null;
        return false;
    }
}