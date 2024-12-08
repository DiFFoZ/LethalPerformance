#if ENABLE_PROFILER
using System.Collections.Generic;
using ES3Internal;
using HarmonyLib;
using Unity.Profiling;

namespace LethalPerformance.Metrics;
[HarmonyPatch]
internal static class Patch_ES3Writer
{
    private static readonly Stack<ProfilerMarker> s_ProfilerStack = new();

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ES3Writer), nameof(ES3Writer.Create),
        argumentTypes: [typeof(ES3Settings), typeof(bool), typeof(bool), typeof(bool)])]
    public static void Create(ES3Settings settings)
    {
        var sampleName = "DiFFoZ.ES3.CreateWriter." + settings.path;

        ProfilerMarker marker = new ProfilerMarker(sampleName);
        marker.Begin();

        s_ProfilerStack.Push(marker);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ES3Writer), nameof(ES3Writer.Create),
        argumentTypes: [typeof(ES3Settings), typeof(bool), typeof(bool), typeof(bool)])]
    public static void CreatePostfix(ES3Writer __result)
    {
        // if file doesn't exists then it returns null reader.
        // if reader is null then end the profiler marker
        if (__result == null)
        {
            Dispose();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ES3JSONWriter), nameof(ES3JSONWriter.Dispose))]
    public static void Dispose()
    {
        if (s_ProfilerStack.TryPop(out var marker))
        {
            marker.End();
        }
    }
}
#endif