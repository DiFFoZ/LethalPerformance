using System.Collections.Generic;
using HarmonyLib;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace LethalPerformance.Metrics;
[HarmonyPatch(typeof(ProfilingScope))]
[IgnoredByDeepProfiler]
internal static class Patch_ProfilingScope
{
    private static readonly Stack<ProfilingScopeData> s_ProfilingScopeDatas = [];

    [HarmonyPrepare]
    public static bool ShouldPatch()
    {
        return Debug.isDebugBuild;
    }

    [HarmonyPatch(MethodType.Constructor, [typeof(CommandBuffer), typeof(ProfilingSampler)])]
    [HarmonyPrefix]
    public static void StartProfiler(CommandBuffer cmd, ProfilingSampler? sampler)
    {
        var data = UnsafeGenericPool<ProfilingScopeData>.Get();
        data.m_Sampler = sampler;
        data.m_CommandBuffer = cmd;

        s_ProfilingScopeDatas.Push(data);
        sampler?.Begin(cmd);
    }

    [HarmonyPatch(nameof(ProfilingScope.Dispose))]
    [HarmonyPrefix]
    public static void StopProfiler()
    {
        if (!s_ProfilingScopeDatas.TryPop(out var data))
        {
            return;
        }

        data.m_Sampler?.End(data.m_CommandBuffer);

        data.m_Sampler = null;
        data.m_CommandBuffer = null!;

        UnsafeGenericPool<ProfilingScopeData>.Release(data);
    }

    private sealed class ProfilingScopeData
    {
        public CommandBuffer m_CommandBuffer = null!;
        public ProfilingSampler? m_Sampler;
    }
}