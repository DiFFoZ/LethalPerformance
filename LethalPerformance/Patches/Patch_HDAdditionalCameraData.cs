using HarmonyLib;
using UnityEngine.Rendering.HighDefinition;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(HDAdditionalCameraData))]
internal static class Patch_HDAdditionalCameraData
{
    [HarmonyPatch(nameof(HDAdditionalCameraData.RegisterDebug))]
    [HarmonyPrefix]
    public static bool DisableRegisteringDebugData()
    {
        // why on earth Unity registering debug hierarchy window even if you cannot access them (in release build).
        return false;
    }
}
