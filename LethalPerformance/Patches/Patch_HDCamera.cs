using System;
using HarmonyLib;
using LethalPerformance.API;
using LethalPerformance.Unity;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(HDCamera))]
internal static unsafe class Patch_HDCamera
{
    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(HDCamera.UpdateShaderVariablesXRCB))]
    public static unsafe bool Prefix(HDCamera __instance, ref ShaderVariablesXR cb)
    {
        fixed (void* p = &cb, views = __instance.m_XRViewConstants)
        {
            Testing.Test((Testing.ReadableViewConstants*)views, __instance.viewCount, (Testing.ReadableShaderVariablesXR*)p);
            return false;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(HDCamera.UpdateShaderVariablesGlobalCB),
        [typeof(ShaderVariablesGlobal), typeof(int)], [ArgumentType.Ref, ArgumentType.Normal])]
    public static unsafe bool UpdateShaderVariablesGlobalCB(HDCamera __instance, ref ShaderVariablesGlobal cb, int frameCount)
    {
        fixed (void* p = &cb, mainViewConstants = &__instance.mainViewConstants, frustumPlaneEquations = __instance.frustumPlaneEquations)
        {
            var vectorParams1 = new float4x4(__instance.screenSize, __instance.postProcessScreenSize,
                RTHandles.rtHandleProperties.rtHandleScale, __instance.m_HistoryRTSystem.rtHandleProperties.rtHandleScale);

            var vectorParams2 = new float4x4(__instance.m_PostProcessRTScales, __instance.m_PostProcessRTScalesHistory,
                new float4(__instance.actualWidth, __instance.finalViewport.width, __instance.actualHeight, __instance.finalViewport.height),
                __instance.zBufferParams);

            var vectorParams3 = new float4x4(__instance.projectionParams, __instance.unity_OrthoParams,
                __instance.screenParams, __instance.taaJitter);

            var isAdditionalDataNull = __instance.m_AdditionalCameraData == null;
            var deExposureMultiplier = isAdditionalDataNull ? 1f : __instance.m_AdditionalCameraData!.deExposureMultiplier;
            var screenCoordScaleBias = isAdditionalDataNull ? new float4(1f, 1f, 0f, 0f) : (float4)__instance.m_AdditionalCameraData!.screenCoordScaleBias;
            var screenSizeOverride = isAdditionalDataNull ? float4.zero : (float4)__instance.m_AdditionalCameraData!.screenSizeOverride;

            Testing.UpdateShaderVariablesGlobalCB((Testing.ReadableShaderVariablesGlobal*)p, __instance.frameSettings, __instance.antialiasing, __instance.camera.cameraType,
                (Testing.ReadableViewConstants*)mainViewConstants, vectorParams1, vectorParams2, vectorParams3, (float4*)frustumPlaneEquations,
                __instance.taaSharpenStrength, __instance.taaFrameIndex, __instance.colorPyramidHistoryMipCount,
                __instance.globalMipBias, __instance.time, __instance.lastTime, frameCount, __instance.viewCount,
                __instance.probeRangeCompressionFactor, deExposureMultiplier, screenCoordScaleBias, !isAdditionalDataNull, screenSizeOverride);
            return false;
        }
    }
}
