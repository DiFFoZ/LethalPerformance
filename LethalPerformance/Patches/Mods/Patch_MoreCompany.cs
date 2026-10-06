using BepInEx.Bootstrap;
using HarmonyLib;
using LethalPerformance.Audio;
using LethalPerformance.Patcher.API;
using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace LethalPerformance.Patches.Mods;

internal static class Patch_MoreCompany
{
    private static readonly MethodInfo? s_MethodToPatch;
    private static bool? s_IsMixerPatched;

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
        CacheIfMixerIsPatched();

        if (!DiageticVoiceMixerNative.HasExpandedVoiceBuses || s_IsMixerPatched!.Value == false)
        {
            return true;
        }

        __result = true;
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void CacheIfMixerIsPatched()
    {
        if (s_IsMixerPatched != null)
        {
            return;
        }

        s_IsMixerPatched = SoundManager.Instance.diageticMixer.GetFloat("PlayerVolume4", out _);
        if (!s_IsMixerPatched.Value)
        {
            StartOfRound.Instance.StartCoroutine(ShowWarning());
        }
        return;
    }

    private static IEnumerator ShowWarning()
    {
        while (HUDManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
        {
            yield return null;
        }

        yield return new WaitForSeconds(5);

        while (GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen)
        {
            yield return null;
        }

        while (HUDManager.Instance.tipsPanelBody.isActiveAndEnabled)
        {
            yield return null;
        }

        HUDManager.Instance.DisplayTip("Lethal Performance", "Failed to patch audio mixer. Report to the developer!", isWarning: true, prefsKey: "LP_MXBAD");
    }
}
