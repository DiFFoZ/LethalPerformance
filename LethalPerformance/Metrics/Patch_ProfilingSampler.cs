#if ENABLE_PROFILER
using HarmonyLib;
using Unity.Profiling;
using UnityEngine.Rendering;

namespace LethalPerformance.Metrics;
[HarmonyPatch(typeof(ProfilingSampler))]
[IgnoredByDeepProfiler]
internal static class Patch_ProfilingSampler
{
    [HarmonyPatch(nameof(ProfilingSampler.Begin))]
    [HarmonyPostfix]
    public static void Begin(ProfilingSampler __instance)
    {
        __instance.inlineSampler?.Begin();
    }

    [HarmonyPatch(nameof(ProfilingSampler.End))]
    [HarmonyPostfix]
    public static void End(ProfilingSampler __instance)
    {
        __instance.inlineSampler?.End();
    }
}
#endif
