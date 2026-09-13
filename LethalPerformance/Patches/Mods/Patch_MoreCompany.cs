using System;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using LethalPerformance.Audio;
using LethalPerformance.Patcher.API;

namespace LethalPerformance.Patches.Mods;

[HarmonyPatch]
internal static class Patch_MoreCompany
{
    private static readonly MethodInfo? s_MethodToPatch;

    static Patch_MoreCompany()
    {
        if (!Chainloader.PluginInfos.TryGetValue(Dependencies.MoreCompany, out var pluginInfo)
            || pluginInfo.Instance == null)
        {
            return;
        }

        var patchClass = pluginInfo.Instance.GetType().Assembly.GetType("MoreCompany.AudioMixerSetFloatPatch");
        if (patchClass != null)
        {
            s_MethodToPatch = AccessTools.Method(patchClass, "Prefix");
        }

        if (s_MethodToPatch == null)
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning("Failed to find MoreCompany AudioMixer.SetFloat patch");
        }
    }

    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyPrepare]
    public static bool ShouldPatch()
    {
        return s_MethodToPatch != null;
    }

    [HarmonyTargetMethod]
    public static MethodBase GetTargetMethod()
    {
        return s_MethodToPatch!;
    }

    [HarmonyPrefix]
    internal static bool SkipSharedBusWorkaround(ref bool __result)
    {
        if (!DiageticVoiceMixerNative.HasExpandedVoiceBuses)
        {
            return true;
        }

        __result = true;
        return false;
    }
}
