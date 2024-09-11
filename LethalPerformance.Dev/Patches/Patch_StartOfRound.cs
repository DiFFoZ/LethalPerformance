using HarmonyLib;
using LethalPerformance.Patcher.API;

namespace LethalPerformance.Dev.Patches;
[HarmonyPatch(typeof(StartOfRound))]
internal static class Patch_StartOfRound
{
    [InitializeOnAwake]
    public static void ListenForConfigChanges()
    {
        LethalPerformanceDevPlugin.Instance.Config.OverriddenSeed.SettingChanged += (_, _) => OverrideSeed();
    }

    [HarmonyPatch(nameof(StartOfRound.Start))]
    [HarmonyPostfix]
    public static void OverrideSeed()
    {
        var newSeed = LethalPerformanceDevPlugin.Instance.Config.OverriddenSeed.Value;
        if (newSeed <= 0)
        {
            StartOfRound.Instance.overrideRandomSeed = false;
            return;
        }

        StartOfRound.Instance.overrideRandomSeed = true;
        StartOfRound.Instance.overrideSeedNumber = newSeed;
    }

    [HarmonyPatch(nameof(StartOfRound.PlayFirstDayShipAnimation))]
    [HarmonyPrefix]
    public static bool DisableSpeaker()
    {
        return false;
    }
}
