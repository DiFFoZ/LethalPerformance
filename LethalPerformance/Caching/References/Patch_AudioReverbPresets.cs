using HarmonyLib;

namespace LethalPerformance.Caching.References;
[HarmonyPatch(typeof(AudioReverbPresets))]
internal static class Patch_AudioReverbPresets
{
    private static readonly UnsafeCachedInstance<AudioReverbPresets> s_Instance
        = UnsafeCacheManager.AddReferenceToMap(new ManualCachedInstance<AudioReverbPresets>());

    // to initialize .cctor
    [HarmonyPrepare]
    public static void Prepare() { }

    [HarmonyPatch("Awake")] // was added by preloader
    [HarmonyPrefix]
    public static void Awake(AudioReverbPresets __instance)
    {
        s_Instance.SetInstance(__instance);
    }
}
