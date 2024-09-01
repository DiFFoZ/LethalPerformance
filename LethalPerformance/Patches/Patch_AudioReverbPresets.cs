using HarmonyLib;
using LethalPerformance.Utilities;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(AudioReverbPresets))]
internal static class Patch_AudioReverbPresets
{
    private static readonly UnsafeCachedInstance<AudioReverbPresets> s_Instance = new();

    static Patch_AudioReverbPresets()
    {
        UnsafeCacheManager.AddReferenceToMap(s_Instance);
    }

    [HarmonyPatch("Awake")] // was added by preloader
    [HarmonyPostfix]
    public static void Awake(AudioReverbPresets __instance)
    {
        s_Instance.Instance = __instance;
    }
}
