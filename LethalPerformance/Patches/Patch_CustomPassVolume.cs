using HarmonyLib;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace LethalPerformance.Patches;

[HarmonyPatch(typeof(CustomPassVolume))]
internal static class Patch_CustomPassVolume
{
    [HarmonyPatch(nameof(CustomPassVolume.Cull))]
    [HarmonyPrefix]
    public static bool Cull(out CullingResults? __result)
    {
        // Disabling custom pass culling makes it use shared cull 
        // CustomPassVolume.Update(hDCamera);

        __result = null;
        return false;
    }
}