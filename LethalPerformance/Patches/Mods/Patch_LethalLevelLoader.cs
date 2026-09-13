using System;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using LethalPerformance.Patcher.API;
using LethalPerformance.Utilities;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace LethalPerformance.Patches.Mods;

[HarmonyPatch]
internal static class Patch_LethalLevelLoader
{
    private static readonly MethodInfo? s_MethodToPatch;

    static Patch_LethalLevelLoader()
    {
        if (!Chainloader.PluginInfos.TryGetValue(Dependencies.LethalLevelLoader, out var pluginInfo)
            || pluginInfo.Instance == null)
        {
            return;
        }

        var patchClass = pluginInfo.Instance.GetType().Assembly.GetType("LethalLevelLoader.Patches");
        if (patchClass != null)
        {
            s_MethodToPatch = AccessTools.Method(patchClass, "OnSceneLoaded");
        }

        if (s_MethodToPatch == null)
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning("Failed to find LethalLevelLoader LethalLevelLoader.Patches.OnSceneLoaded patch");
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
    internal static void OptimizeAudioSources(Scene scene)
    {
        // From what I understand not used audiomixer's from mods still processed with all effects,
        // and one of effect is pitch shifter that causing high processing time compared to other effects.
        // 
        // So finding all audio mixers and deleting them.

        // TODO:
        // move it after generating dungeon (dawn lib is now hotloading dungeons)

        if (SoundManager.Instance == null)
        {
            // Main scene yet
            return;
        }
        
        // LethalLib might still not applied audio fix, so wait until moon is loaded
        if (scene.IsSceneShip())
        {
            return;
        }

        var gameMixer = SoundManager.Instance.diageticMixer;
        var musicMixer = GameObject.Find("/Systems/Audios/Music1").GetComponent<AudioSource>().outputAudioMixerGroup.audioMixer;

        if (gameMixer == null || musicMixer == null)
        {
            return;
        }

        var mixers = Resources.FindObjectsOfTypeAll<AudioMixer>();
        var shapshots = Resources.FindObjectsOfTypeAll<AudioMixerSnapshot>();
        var groups = Resources.FindObjectsOfTypeAll<AudioMixerGroup>();

        foreach (var snapshot in shapshots)
        {
            var mixer = snapshot.audioMixer;
            if (mixer == gameMixer || mixer == musicMixer)
            {
                LethalPerformancePlugin.Instance.Logger.LogDebug($"skipped real {mixer.name}");
                continue;
            }

            UnityEngine.Object.DestroyImmediate(snapshot, true);
        }

        foreach (var group in groups)
        {
            var mixer = group.audioMixer;
            if (mixer == gameMixer || mixer == musicMixer)
            {
                LethalPerformancePlugin.Instance.Logger.LogDebug($"skipped real {mixer.name}");
                continue;
            }

            UnityEngine.Object.DestroyImmediate(group, true);
        }

        foreach (var mixer in mixers)
        {
            if (mixer == gameMixer || mixer == musicMixer)
            {
                LethalPerformancePlugin.Instance.Logger.LogDebug($"skipped real {mixer.name}");
                continue;
            }

            UnityEngine.Object.DestroyImmediate(mixer, true);
        }
    }
}
