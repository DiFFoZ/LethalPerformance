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
    private const int c_DrunknessSnapshotId = 4;
    private const float c_PitchEpsilon = 0.025f;

    private static AudioMixerGroup? s_SfxGroup;
    private static AudioMixerGroup[]? s_VoiceGroups;
    private static bool[]? s_VoiceBypass;
    private static bool s_SfxBypass = true;
    private static bool s_Ready;

    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyPatch(nameof(SoundManager.Start))]
    [HarmonyPostfix]
    private static void Start(SoundManager __instance)
    {
        if (!UnityAudioMixerNative.TryInitialize())
        {
            return;
        }

        s_VoiceGroups = __instance.playerVoiceMixers;
        s_SfxGroup = FindSfxGroup(__instance.diageticMixer);
        s_VoiceBypass = s_VoiceGroups == null ? null : new bool[s_VoiceGroups.Length];
        s_Ready = s_VoiceGroups != null;

        if (!s_Ready)
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning("Diagetic voice mixer groups were not found");
            return;
        }

        ApplyVoiceBypass(__instance, force: true);
        ApplySfxBypass(__instance, force: true);
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

    [HarmonyPatch(nameof(SoundManager.SetDiageticMixerSnapshot))]
    [HarmonyPostfix]
    private static void SetDiageticMixerSnapshot(SoundManager __instance)
    {
        ApplySfxBypass(__instance, force: false);
    }

    [HarmonyPatch(nameof(SoundManager.ResumeCurrentMixerSnapshot))]
    [HarmonyPostfix]
    private static void ResumeCurrentMixerSnapshot(SoundManager __instance)
    {
        ReapplyAfterSnapshot(__instance);
    }

    internal static void ReapplyAfterSnapshot(SoundManager instance)
    {
        ApplySfxBypass(instance, force: true);
        ApplyVoiceBypass(instance, force: true);
    }

    private static void ApplyVoiceBypass(SoundManager instance, bool force)
    {
        if (!s_Ready || s_VoiceGroups == null || s_VoiceBypass == null)
        {
            return;
        }

        var pitches = instance.playerVoicePitches;
        var offsets = instance.pitchOffsets;
        var players = StartOfRound.Instance?.allPlayerScripts;

        var count = Math.Min(s_VoiceGroups.Length, pitches?.Length ?? 0);
        for (var i = 0; i < count; i++)
        {
            var pitch = pitches![i];
            if (offsets != null && i < offsets.Length && offsets[i] != 0f
                && players != null && i < players.Length && players[i] != null)
            {
                pitch += offsets[i] * Mathf.Abs(players[i].health / 100f - 1f);
            }

            var bypass = Mathf.Abs(pitch - 1f) <= c_PitchEpsilon;
            if (!force && bypass == s_VoiceBypass[i])
            {
                continue;
            }

            bypass = true;

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

    private static void ApplySfxBypass(SoundManager instance, bool force)
    {
        if (!s_Ready || s_SfxGroup == null)
        {
            return;
        }

        var bypass = instance.currentMixerSnapshotID != c_DrunknessSnapshotId;
        if (!force && bypass == s_SfxBypass)
        {
            return;
        }

        if (UnityAudioMixerNative.SetEffectBypass(s_SfxGroup, MixerEffect.PitchShifter, bypass))
        {
            s_SfxBypass = bypass;
#if ENABLE_PROFILER
            LethalPerformancePlugin.Instance.Logger.LogInfo(
                $"Pitch Shifter bypass={bypass} on SFX (snapshot={instance.currentMixerSnapshotID})");
#endif
        }
    }

    private static AudioMixerGroup? FindSfxGroup(AudioMixer mixer)
    {
        if (mixer == null)
        {
            return null;
        }

        var groups = mixer.FindMatchingGroups("SFX");
        return groups is { Length: > 0 } ? groups[0] : null;
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
