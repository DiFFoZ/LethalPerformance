using HarmonyLib;
using LethalLevelLoader;
using UnityEngine;
using UnityEngine.Audio;
using LLLPatches = LethalLevelLoader.Patches;

namespace LethalPerformance.LLLCompact.Patches;
[HarmonyPatch(typeof(LLLPatches))]
internal static class Patch_Patches
{
    [HarmonyPatch(nameof(LLLPatches.OnSceneLoaded))]
    [HarmonyPostfix]
    public static void OnSceneLoaded()
    {
        ExtendedLevel currentLevel = LevelManager.CurrentExtendedLevel;
        if (currentLevel == null || !currentLevel.IsLevelLoaded)
        {
            return;
        }

        if (currentLevel.ContentType is ContentType.Vanilla or ContentType.External or ContentType.Any)
        {
            return;
        }

        var gameMixer = SoundManager.Instance.diageticMixer;
        var musicMixer = GameObject.Find("/Systems/Audios/Music1").GetComponent<AudioSource>().outputAudioMixerGroup.audioMixer;

        var lllGroups = OriginalContent.AudioMixerGroups;
        LethalLevelPerformanceLLLCompactPlugin.Instance.Logger.LogInfo(lllGroups.Count.ToString());
        foreach (var group in lllGroups)
        {
            LethalLevelPerformanceLLLCompactPlugin.Instance.Logger.LogInfo(group.name);
            if (group.audioMixer != gameMixer && group.audioMixer != musicMixer)
            {
                LethalLevelPerformanceLLLCompactPlugin.Instance.Logger.LogInfo("Fake group " + group.name);
            }
        }

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
                LethalLevelPerformanceLLLCompactPlugin.Instance.Logger.LogDebug($"skipped real {mixer.name}");
                continue;
            }

            UnityEngine.Object.DestroyImmediate(snapshot, true);
        }

        foreach (var group in groups)
        {
            var mixer = group.audioMixer;
            if (mixer == gameMixer || mixer == musicMixer)
            {
                LethalLevelPerformanceLLLCompactPlugin.Instance.Logger.LogDebug($"skipped real {mixer.name}");
                continue;
            }

            UnityEngine.Object.DestroyImmediate(group, true);
        }

        foreach (var mixer in mixers)
        {
            if (mixer == gameMixer || mixer == musicMixer)
            {
                LethalLevelPerformanceLLLCompactPlugin.Instance.Logger.LogDebug($"skipped real {mixer.name}");
                continue;
            }

            UnityEngine.Object.DestroyImmediate(mixer, true);
        }
    }
}
