using HarmonyLib;

namespace LethalPerformance.Dev.Patches;
[HarmonyPatch(typeof(StartOfRound))]
internal static class Patch_StartOfRound
{
    [HarmonyPatch(nameof(StartOfRound.Start))]
    [HarmonyPostfix]
    public static void OverrideSeed(StartOfRound __instance)
    {
        var newSeed = LethalPerformanceDevPlugin.Instance.Config.OverriddenSeed.Value;
        if (newSeed <= 0)
        {
            return;
        }

        __instance.overrideRandomSeed = true;
        __instance.overrideSeedNumber = newSeed;
    }
}
