#if ENABLE_PROFILER
using System.Collections.Generic;
using ES3Internal;
using HarmonyLib;
using Unity.Profiling;

namespace LethalPerformance.Metrics;
[HarmonyPatch]
[HarmonyPriority(Priority.Last)]
internal static class Patch_ES3Reader
{
    private static readonly Stack<ProfilerMarker> s_ProfilerStack = new();

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ES3Reader), nameof(ES3Reader.Create), argumentTypes: [typeof(ES3Settings)])]
    public static void Create(ES3Settings settings)
    {
        var sampleName = "DiFFoZ.ES3.Reader." + settings.path;

        ProfilerMarker marker = new ProfilerMarker(sampleName);
        marker.Begin();

        s_ProfilerStack.Push(marker);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ES3Reader), nameof(ES3Reader.Create), argumentTypes: [typeof(ES3Settings)])]
    public static void CreatePostfix(ES3Reader __result)
    {
        // if file doesn't exists then it returns null reader.
        // if reader is null then end the profiler marker
        if (__result == null)
        {
            Dispose();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ES3JSONReader), nameof(ES3JSONReader.Dispose))]
    public static void Dispose()
    {
        if (s_ProfilerStack.TryPop(out var marker))
        {
            marker.End();
        }
    }
}
#endif