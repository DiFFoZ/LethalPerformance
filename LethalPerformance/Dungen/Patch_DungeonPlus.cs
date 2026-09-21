using System.Runtime.CompilerServices;
using DunGenPlus.Generation;
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

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void ResetDictionary()
    {
        TileProxyPatch.ResetDictionary();
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static bool IsDunGenPlusActive()
    {
        return DunGenPlusGenerator.Active;
    }
}