using DunGenPlus.Patches;
using HarmonyLib;

namespace LethalPerformance.Dungen;

internal static class Patch_DungeonPlus
{
    public static bool AllowReset;

    [HarmonyPatch(typeof(TileProxyPatch), nameof(TileProxyPatch.ResetDictionary))]
    [HarmonyPrefix]
    public static bool DontResetDictionary()
    {
        return AllowReset;
    }

    public static void ResetDictionary()
    {
        TileProxyPatch.ResetDictionary();
    }
}