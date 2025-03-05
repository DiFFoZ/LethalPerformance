using System;
using System.Diagnostics;
using HarmonyLib;
using LethalPerformance.Caching;
using LethalPerformance.Patcher.API;
using UnityEngine;
using UnityEngine.Profiling;

namespace LethalPerformance.Caching.FindingObjectOptimization;

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
    public static bool FindObjectFast(Type type, FindObjectsInactive findObjectsInactive, out Object? __result)
    {
        ShowInProfilerType(type, false);
        return !TryFindObjectFast(type, findObjectsInactive, out __result);
    }

    [HarmonyPatch(nameof(Object.FindObjectOfType), [typeof(Type), typeof(bool)])]
    [HarmonyPrefix]
    public static bool FindObjectFast(Type type, bool includeInactive, out Object? __result)
    {
        ShowInProfilerType(type, false);
        return !TryFindObjectFast(type, includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            out __result);
    }

    [HarmonyPatch(nameof(Object.FindObjectsOfType), [typeof(Type), typeof(bool)])]
    [HarmonyPrefix]
    public static bool FindObjectsFast(Type type, bool includeInactive, out Object[]? __result)
    {
        ShowInProfilerType(type, true);
        return !TryFindObjectsFast(type, includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            out __result);
    }

    [HarmonyPatch(nameof(Object.FindObjectsByType), [typeof(Type), typeof(FindObjectsInactive), typeof(FindObjectsSortMode)])]
    [HarmonyPrefix]
    public static bool FindObjectsFast(Type type, FindObjectsInactive findObjectsInactive, out Object[]? __result)
    {
        ShowInProfilerType(type, true);
        return !TryFindObjectsFast(type, findObjectsInactive, out __result);
    }

    [Conditional("ENABLE_PROFILER")]
    private static void ShowInProfilerType(Type type, bool findAllObjects)
    {
        var name = "DiFFoZ.Find." + type.Name;
        if (findAllObjects)
        {
            name += " (all objects)";
        }

        LethalPerformancePlugin.Instance.Logger.LogInfo("[Cache] " + name);
        if (findAllObjects)
        {
            //LethalPerformancePlugin.Instance.Logger.LogDebug("[Cache] " + Environment.StackTrace);
        }

        Profiler.BeginSample(name);
        Profiler.EndSample();
    }

    public static bool TryFindObjectFast(Type type, FindObjectsInactive findObjectsInactive, out Object? result)
    {
        if (UnsafeCacheManager.TryGetCachedReference(type, findObjectsInactive, out result))
        {
            return true;
        }

#if ENABLE_PROFILER
        LethalPerformancePlugin.Instance.Logger.LogWarning($"Failed to find cached {type.Name} object");
#endif

        result = null;
        return false;
    }

    public static bool TryFindObjectsFast(Type type, FindObjectsInactive findObjectsInactive, out Object[]? result)
    {
        if (UnsafeCacheManager.TryGetCachedReferences(type, findObjectsInactive, out result))
        {
            return true;
        }

#if ENABLE_PROFILER
        LethalPerformancePlugin.Instance.Logger.LogWarning($"Failed to find cached {type.Name} objects");
#endif

        result = null;
        return false;
    }
}