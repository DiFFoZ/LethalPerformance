using HarmonyLib;
using Unity.Burst;
using UnityEngine.Rendering.HighDefinition;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(HDCamera), nameof(HDCamera.UpdateShaderVariablesXRCB))]
internal static unsafe class Patch_HDCamera
{
    private static readonly Testing.TestDelegate s_TestDelegate = BurstCompiler.CompileFunctionPointer<Testing.TestDelegate>(Testing.Test).Invoke;

    [HarmonyPrefix]
    public static unsafe bool Prefix(HDCamera __instance, ref ShaderVariablesXR cb)
    {
        fixed (void* p = &cb, views = __instance.m_XRViewConstants)
        {
            s_TestDelegate((Testing.ReadableViewConstants*)views, __instance.viewCount, (Testing.ReadableShaderVariablesXR*)p);

            cb = *(ShaderVariablesXR*)p;
            return false;
        }
    }
}
