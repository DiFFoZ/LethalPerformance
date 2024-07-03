using HarmonyLib;
using Unity.Profiling;

namespace LethalPerformance.PerformanceMetrics;
#if ENABLE_PROFILER
[HarmonyPatch(typeof(StartOfRound))]
[HarmonyPriority(Priority.Last)]
#endif
internal static class Patch_StartOfRoundPerformanceTest
{
    private static readonly ProfilerMarker s_SetPlayerSafeInShip = new("DiFFoZ.StartOfRound.SetPlayerSafeInShip");

    [HarmonyPatch(nameof(StartOfRound.SetPlayerSafeInShip))]
    [HarmonyPrefix]
    private static void SetPlayerSafeInShipBefore()
    {
        s_SetPlayerSafeInShip.Begin();
    }

    [HarmonyPatch(nameof(StartOfRound.SetPlayerSafeInShip))]
    [HarmonyPostfix]
    private static void SetPlayerSafeInShipAfter()
    {
        s_SetPlayerSafeInShip.End();
    }
}
