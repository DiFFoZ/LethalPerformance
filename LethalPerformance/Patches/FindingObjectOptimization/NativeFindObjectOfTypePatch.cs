using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace LethalPerformance.Patches.FindingObjectOptimization;

[HarmonyPatch]
internal static class NativeFindObjectOfTypePatch
{
    private static readonly Dictionary<Type, Func<Object>> s_MapGettingInstance = new()
    {
        { typeof(StartOfRound), () => StartOfRound.Instance },
        { typeof(TimeOfDay), () => TimeOfDay.Instance },
        { typeof(GameNetworkManager), () => GameNetworkManager.Instance },
        { typeof(HUDManager), () => HUDManager.Instance },
        { typeof(GlobalEffects), () => GlobalEffects.Instance },
        { typeof(IngamePlayerSettings), () => IngamePlayerSettings.Instance },
        { typeof(RoundManager), () => RoundManager.Instance },
        { typeof(ShipBuildModeManager), () => ShipBuildModeManager.Instance },
        { typeof(SoundManager), () => SoundManager.Instance },
        { typeof(SteamManager), () => SteamManager.Instance },
    };

    [HarmonyTargetMethods]
    public static IEnumerable<MethodInfo> GetTargetMethods()
    {
        yield return typeof(Object).GetMethod(nameof(Object.FindObjectOfType), [typeof(Type), typeof(bool)]);
        yield return typeof(Object).GetMethod(nameof(Object.FindAnyObjectByType), [typeof(Type), typeof(FindObjectsInactive)]);
    }

    [HarmonyPrefix]
    public static bool FindObjectFast(Type type, ref Object __result)
    {
        if (s_MapGettingInstance.TryGetValue(type, out var getter))
        {
            __result = getter();

            if (__result.m_CachedPtr != IntPtr.Zero)
            {
                return false;
            }
        }

        return true;
    }
}