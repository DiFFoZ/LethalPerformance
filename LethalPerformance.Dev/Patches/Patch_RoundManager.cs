using HarmonyLib;

namespace LethalPerformance.Dev.Patches;
[HarmonyPatch(typeof(RoundManager))]
internal static class Patch_RoundManager
{
    [HarmonyPatch(nameof(RoundManager.BeginEnemySpawning))]
    [HarmonyPrefix]
    public static bool DisableSpawning()
    {
        return LethalPerformanceDevPlugin.Instance.Config.ShouldSpawnEnemies.Value;
    }
}
