#if ENABLE_PROFILER
using System.Collections.Generic;
using ES3Internal;
using HarmonyLib;
using UnityEngine.Profiling;

namespace LethalPerformance.Metrics;
[HarmonyPatch()]
internal static class Patch_ES3Reader
{
    private static readonly Stack<string> s_ProfilerStack = new();

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ES3Reader), nameof(ES3Reader.Create), argumentTypes: [typeof(ES3Settings)])]
    public static void Create(ES3Settings settings)
    {
        var sampleName = "DiFFoZ.ES3.Create." + settings.path;

        s_ProfilerStack.Push(sampleName);
        Profiler.BeginSample(sampleName);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ES3JSONReader), nameof(ES3JSONReader.Dispose))]
    public static void Dispose()
    {
        if (s_ProfilerStack.TryPop(out _))
        {
            Profiler.EndSample();
        }
    }
}
#endif