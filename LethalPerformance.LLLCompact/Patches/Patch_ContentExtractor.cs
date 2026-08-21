using System.Linq;
using HarmonyLib;
using LethalLevelLoader;
using UnityEngine;

namespace LethalPerformance.LLLCompact.Patches;
[HarmonyPatch(typeof(ContentExtractor))]
internal static class Patch_ContentExtractor
{
    [HarmonyPatch(nameof(ContentExtractor.ExtractMemoryLoadedAudioMixerGroups))]
    [HarmonyPrefix]
    public static bool ExtractMemoryLoadedAudioMixerGroups()
    {
        // LLL was using wrong audio mixer groups (check Patch_Patches how it's validated)

        var gameMixer = GameObject.Find("/Systems/Audios/DiageticBackground").GetComponent<AudioSource>().outputAudioMixerGroup.audioMixer;
        var musicMixer = GameObject.Find("/Systems/Audios/Music1").GetComponent<AudioSource>().outputAudioMixerGroup.audioMixer;

        OriginalContent.AudioMixerGroups.Clear();

        OriginalContent.AudioMixerGroups.AddRange(gameMixer.FindMatchingGroups(string.Empty)
            .Concat(musicMixer.FindMatchingGroups(string.Empty)));

        // not used
        OriginalContent.AudioMixerSnapshots.Clear();


        return false;
    }
}
