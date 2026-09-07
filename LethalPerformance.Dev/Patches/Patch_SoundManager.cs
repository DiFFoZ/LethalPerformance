using System;
using HarmonyLib;
using LethalPerformance.Patcher.API;
using UnityEngine;

namespace LethalPerformance.Dev.Patches;

[HarmonyPatch(typeof(SoundManager))]
internal static class Patch_SoundManager
{
    private static bool IsEnabled()
    {
        return LethalPerformanceDevPlugin.Instance.Config.SecondPlayerUsesLastVoiceGroup.Value;
    }

    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyPatch(nameof(SoundManager.Start))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void Start(SoundManager __instance)
    {
        AssignSecondPlayerToLastVoiceGroup(__instance);
    }

    [HarmonyPatch(nameof(SoundManager.SetPlayerPitch))]
    [HarmonyPrefix]
    private static void SetPlayerPitch(ref int playerObjNum)
    {
        if (!IsEnabled() || playerObjNum != 1)
        {
            return;
        }

        var last = GetLastVoiceIndex();
        if (last > 1)
        {
            playerObjNum = last;
        }
    }

    private static void AssignSecondPlayerToLastVoiceGroup(SoundManager sound)
    {
        if (!IsEnabled())
        {
            return;
        }

        var mixers = sound.playerVoiceMixers;
        if (mixers == null || mixers.Length <= 1 + 1)
        {
            return;
        }

        var last = mixers.Length - 1;
        if (mixers[last] == null || mixers[1] == mixers[last])
        {
            return;
        }

        (mixers[1], mixers[last]) = (mixers[last], mixers[1]);

        Debug.Log($"[LP Dev] Second player uses voice group {last} ({mixers[1].name})");
    }

    private static int GetLastVoiceIndex()
    {
        var mixers = SoundManager.Instance?.playerVoiceMixers;
        if (mixers == null || mixers.Length == 0)
        {
            return 1;
        }

        return mixers.Length - 1;
    }
}
