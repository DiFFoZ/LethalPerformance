using System;
using HarmonyLib;
using LethalPerformance.Audio;
using LethalPerformance.Patcher.API;
using UnityEngine;
using UnityEngine.Audio;

namespace LethalPerformance.Patches;

[HarmonyPatch(typeof(SoundManager))]
internal static class Patch_SoundManager
{
    private static AudioMixerGroup[]? s_VoiceGroups;
    private static bool[]? s_VoiceBypass;

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
        if (!UnityAudioMixerNative.TryInitialize())
        {
            return;
        }

        s_VoiceGroups = __instance.playerVoiceMixers;
        s_VoiceBypass = new bool[s_VoiceGroups.Length];

        LethalPerformancePlugin.Instance.Logger.LogDebug("Registered " + s_VoiceGroups.Length.ToString() + " voice mixer groups");

        ApplyVoiceBypass(__instance, force: true);
    }

    [HarmonyPatch(nameof(SoundManager.SetPlayerPitch))]
    [HarmonyPostfix]
    private static void SetPlayerPitch(SoundManager __instance)
    {
        ApplyVoiceBypass(__instance, force: false);
    }

    [HarmonyPatch(nameof(SoundManager.SetPlayerVoiceFilters))]
    [HarmonyPostfix]
    private static void SetPlayerVoiceFilters(SoundManager __instance)
    {
        ApplyVoiceBypass(__instance, force: false);
    }

    [HarmonyPatch(nameof(SoundManager.ResumeCurrentMixerSnapshot))]
    [HarmonyPostfix]
    private static void ResumeCurrentMixerSnapshot(SoundManager __instance)
    {
        ReapplyAfterSnapshot(__instance);
    }

    internal static void ReapplyAfterSnapshot(SoundManager instance)
    {
        ApplyVoiceBypass(instance, force: true);
    }

    private static void ApplyVoiceBypass(SoundManager instance, bool force)
    {
        // Copy of SoundManager.SetPlayerVoiceFilters
        if (s_VoiceGroups == null || s_VoiceBypass == null)
        {
            return;
        }

        var pitches = instance.playerVoicePitches;
        var offsets = instance.pitchOffsets;
        var players = StartOfRound.Instance.allPlayerScripts;

        var count = Math.Min(s_VoiceGroups.Length, pitches?.Length ?? 0);
        for (var i = 0; i < count; i++)
        {
            var pitch = pitches![i];
            if (i < offsets.Length && offsets[i] != 0f
                && i < players.Length && players[i] != null)
            {
                pitch += offsets[i] * Mathf.Abs(players[i].health / 100f - 1f);
            }

            var bypass = Mathf.Abs(pitch - 1f) <= 0.025f;
            if (!force && bypass == s_VoiceBypass[i])
            {
                continue;
            }

            if (UnityAudioMixerNative.SetEffectBypass(s_VoiceGroups[i], MixerEffect.PitchShifter, bypass))
            {
                s_VoiceBypass[i] = bypass;
#if ENABLE_PROFILER
                LethalPerformancePlugin.Instance.Logger.LogInfo(
                    $"Pitch Shifter bypass={bypass} on {s_VoiceGroups[i].name} (pitch={pitch:0.###})");
#endif
            }
        }
    }
}

[HarmonyPatch(typeof(AudioMixerSnapshot))]
internal static class Patch_AudioMixerSnapshot
{
    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyPatch(nameof(AudioMixerSnapshot.TransitionTo))]
    [HarmonyPostfix]
    private static void TransitionTo()
    {
        var soundManager = SoundManager.Instance;
        if (soundManager == null)
        {
            return;
        }

        // Snapshot apply can rewrite live DSP bypass from the asset (all Pitch Shifters m_Bypass: 0).
        Patch_SoundManager.ReapplyAfterSnapshot(soundManager);
    }
}
