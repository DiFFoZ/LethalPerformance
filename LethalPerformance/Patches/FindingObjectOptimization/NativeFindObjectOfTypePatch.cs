using System;
using System.Diagnostics;
using HarmonyLib;
using LethalPerformance.Caching;
using LethalPerformance.Patcher.API;
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
        ShowInProfilerType(type, false);
        return !TryFindObjectFast(type, findObjectsInactive, out __result);
    }

    [HarmonyPatch(nameof(Object.FindObjectOfType), [typeof(Type), typeof(bool)])]
    [HarmonyPrefix]
    public static bool FindObjectFast(Type type, bool includeInactive, ref Object? __result)
    {
        ShowInProfilerType(type, false);
        return !TryFindObjectFast(type, includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            out __result);
    }

#if ENABLE_PROFILER
    [HarmonyPatch(nameof(Object.FindObjectsOfType), [typeof(Type), typeof(bool)])]
    [HarmonyPatch(nameof(Object.FindObjectsByType), [typeof(Type), typeof(FindObjectsInactive), typeof(FindObjectsSortMode)])]
    [HarmonyPrefix]
    public static void FindObjectsFast(Type type)
    {
        ShowInProfilerType(type, true);
    }
#endif

    [Conditional("ENABLE_PROFILER")]
    private static void ShowInProfilerType(Type type, bool findAllObjects)
    {
        var name = "DiFFoZ.Find." + type.Name;
        if (findAllObjects)
        {
            name += " (all objects)";
        }

        Profiler.BeginSample(name);
        Profiler.EndSample();
    }

    public static bool TryFindObjectFast(Type type, FindObjectsInactive findObjectsInactive, out Object? __result)
    {
        if (UnsafeCacheManager.TryGetCachedReference(type, findObjectsInactive, out var cache))
        {
            __result = cache;
            return true;
        }

#if true
        LethalPerformancePlugin.Instance.Logger.LogWarning($"Failed to find cached {type.Name} object");
#endif

        __result = null;
        return false;
    }
}