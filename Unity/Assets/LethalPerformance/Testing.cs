using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility;

#if UNITY_2022
[assembly: AssemblyVersion("0.0.4.0")]
#endif

namespace LethalPerformance
{
    [BurstCompile]
    public static unsafe class Testing
    {
        public delegate void TestDelegate(in ReadableViewConstants* xrViewConstants, in int viewCount, ReadableShaderVariablesXR* cb);
        public delegate void UpdateShaderVariablesGlobalCBDelegate(in ReadableShaderVariablesGlobal* cb, in int frameCount,
            in bool isTaaAntialiasingEnabled, in HDCamera.ViewConstants* mainViewConstant, in float4x4 vectorScreens,
            in float4x3 vectorScales, in float4x4 vectorParams, in float4* frustumPlaneEquations,
            in float taaSharpenStrength, in int taaFrameIndex, in float4 taaJitter, in int colorPyramidHistoryMipCount,
            in float globalMipBias, in float4 timeParams, in int viewCount, in float probeRangeCompressionFactor,
            in float deExposureMultiplier, in int transparentCameraOnlyMotionVectors, in float4 screenSizeOverride,
            in float4 screenCoordScaleBias);

        [BurstCompile]
        public static void Log(in FixedString128Bytes log)
        {
#if UNITY_2022
            Debug.Log(log);
#endif
        }

        [BurstCompile]
        public static unsafe void Test(in ReadableViewConstants* xrViewConstants, in int viewCount, ReadableShaderVariablesXR* cb)
        {
#if UNITY_2022
            for (var i = 0; i < viewCount; i++)
            {
                MemCpy(cb->_XRViewMatrix + i * 15, &xrViewConstants[i].viewMatrix, sizeof(float4x4));
                MemCpy(cb->_XRInvViewMatrix + i * 15, &xrViewConstants[i].invViewMatrix, sizeof(float4x4));
                MemCpy(cb->_XRProjMatrix + i * 15, &xrViewConstants[i].projMatrix, sizeof(float4x4));
                MemCpy(cb->_XRInvProjMatrix + i * 15, &xrViewConstants[i].invProjMatrix, sizeof(float4x4));
                MemCpy(cb->_XRViewProjMatrix + i * 15, &xrViewConstants[i].viewProjMatrix, sizeof(float4x4));
                MemCpy(cb->_XRInvViewProjMatrix + i * 15, &xrViewConstants[i].invViewProjMatrix, sizeof(float4x4));
                MemCpy(cb->_XRNonJitteredViewProjMatrix + i * 15, &xrViewConstants[i].nonJitteredViewProjMatrix, sizeof(float4x4));
                MemCpy(cb->_XRPrevViewProjMatrix + i * 15, &xrViewConstants[i].prevViewProjMatrix, sizeof(float4x4));
                MemCpy(cb->_XRPrevInvViewProjMatrix + i * 15, &xrViewConstants[i].prevInvViewProjMatrix, sizeof(float4x4));
                MemCpy(cb->_XRViewProjMatrixNoCameraTrans + i * 15, &xrViewConstants[i].viewProjectionNoCameraTrans, sizeof(float4x4));
                MemCpy(cb->_XRPrevViewProjMatrixNoCameraTrans + i * 15, &xrViewConstants[i].prevViewProjMatrixNoCameraTrans, sizeof(float4x4));
                MemCpy(cb->_XRPixelCoordToViewDirWS + i * 15, &xrViewConstants[i].pixelCoordToViewDirWS, sizeof(float4x4));

                MemCpy(cb->_XRWorldSpaceCameraPos + i * 3, &xrViewConstants[i].worldSpaceCameraPos, sizeof(float4));
                MemCpy(cb->_XRWorldSpaceCameraPosViewOffset + i * 3, &xrViewConstants[i].worldSpaceCameraPosViewOffset, sizeof(float4));
                MemCpy(cb->_XRPrevWorldSpaceCameraPos + i * 3, &xrViewConstants[i].prevWorldSpaceCameraPos, sizeof(float4));
            }
#endif
        }

        [BurstCompile]
        public static unsafe void UpdateShaderVariablesGlobalCB(in ReadableShaderVariablesGlobal* cb, in int frameCount,
            in bool isTaaAntialiasingEnabled, in HDCamera.ViewConstants* mainViewConstant, in float4x4 vectorScreens,
            in float4x3 vectorScales, in float4x4 vectorParams, in float4* frustumPlaneEquations,
            in float taaSharpenStrength, in int taaFrameIndex, in float4 taaJitter, in int colorPyramidHistoryMipCount,
            in float globalMipBias, in float4 timeParams, in int viewCount, in float probeRangeCompressionFactor,
            in float deExposureMultiplier, in int transparentCameraOnlyMotionVectors, in float4 screenSizeOverride,
            in float4 screenCoordScaleBias)
        {
#if UNITY_2022
            cb->_ViewMatrix = mainViewConstant->viewMatrix;
            cb->_CameraViewMatrix = mainViewConstant->viewMatrix;
            cb->_InvViewMatrix = mainViewConstant->invViewMatrix;
            cb->_ProjMatrix = mainViewConstant->projMatrix;
            cb->_InvProjMatrix = mainViewConstant->invProjMatrix;
            cb->_ViewProjMatrix = mainViewConstant->viewProjMatrix;
            cb->_CameraViewProjMatrix = mainViewConstant->viewProjMatrix;
            cb->_InvViewProjMatrix = mainViewConstant->invViewProjMatrix;
            cb->_NonJitteredViewProjMatrix = mainViewConstant->nonJitteredViewProjMatrix;
            cb->_PrevViewProjMatrix = mainViewConstant->prevViewProjMatrix;
            cb->_PrevInvViewProjMatrix = mainViewConstant->prevInvViewProjMatrix;
            cb->_WorldSpaceCameraPos_Internal = mainViewConstant->worldSpaceCameraPos;
            cb->_PrevCamPosRWS_Internal = mainViewConstant->prevWorldSpaceCameraPos;

            cb->_ScreenSize = vectorScreens[0];
            cb->_PostProcessScreenSize = vectorScreens[1];
            cb->_RTHandleScale = vectorScreens[2];
            cb->_RTHandleScaleHistory = vectorScreens[3];
            cb->_RTHandlePostProcessScale = vectorScales[0];
            cb->_RTHandlePostProcessScaleHistory = vectorScales[1];
            cb->_DynamicResolutionFullscreenScale = new Vector4(vectorScales[2][0] / vectorScales[2][1], vectorScales[2][2] / vectorScales[2][3], 0f, 0f);

            cb->_ZBufferParams = vectorParams[0];
            cb->_ProjectionParams = vectorParams[1];
            cb->unity_OrthoParams = vectorParams[2];
            cb->_ScreenParams = vectorParams[3];
            MemCpy(cb->_FrustumPlanes, frustumPlaneEquations, sizeof(float4) * 6);

            cb->_TaaFrameInfo = new Vector4(taaSharpenStrength, 0f, taaFrameIndex, isTaaAntialiasingEnabled ? 1f : 0f);
            cb->_TaaJitterStrength = taaJitter;
            cb->_ColorPyramidLodCount = colorPyramidHistoryMipCount;
            cb->_GlobalMipBias = globalMipBias;
            cb->_GlobalMipBiasPow2 = (float)math.pow(2.0, globalMipBias);

            var time = timeParams[0];
            var lastTime = timeParams[1];
            var deltaTime = timeParams[2];
            var smoothDeltaTime = timeParams[3];

            cb->_Time = new Vector4(time * 0.05f, time, time * 2f, time * 3f);
            cb->_SinTime = new Vector4(math.sin(time * 0.125f), math.sin(time * 0.25f), math.sin(time * 0.5f), math.sin(time));
            cb->_CosTime = new Vector4(math.cos(time * 0.125f), math.cos(time * 0.25f), math.cos(time * 0.5f), math.cos(time));
            cb->unity_DeltaTime = new Vector4(deltaTime, 1f / deltaTime, smoothDeltaTime, 1f / smoothDeltaTime);
            cb->_TimeParameters = new Vector4(time, math.sin(time), math.cos(time), 0f);
            cb->_LastTimeParameters = new Vector4(lastTime, math.sin(lastTime), math.cos(lastTime), 0f);

            cb->_FrameCount = frameCount;
            cb->_XRViewCount = (uint)viewCount;

            cb->_ProbeExposureScale = 1f / math.max(probeRangeCompressionFactor, 1e-6f);
            cb->_DeExposureMultiplier = deExposureMultiplier;
            cb->_TransparentCameraOnlyMotionVectors = transparentCameraOnlyMotionVectors;
            cb->_ScreenSizeOverride = screenSizeOverride;
            cb->_ScreenCoordScaleBias = screenCoordScaleBias;
#endif
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct ReadableViewConstants
        {
            public Matrix4x4 viewMatrix;
            public Matrix4x4 invViewMatrix;
            public Matrix4x4 projMatrix;
            public Matrix4x4 invProjMatrix;
            public Matrix4x4 viewProjMatrix;
            public Matrix4x4 invViewProjMatrix;
            public Matrix4x4 nonJitteredViewProjMatrix;
            public Matrix4x4 prevViewMatrix; // skipped
            public Matrix4x4 prevViewProjMatrix;
            public Matrix4x4 prevInvViewProjMatrix;
            public Matrix4x4 prevViewProjMatrixNoCameraTrans;
            public Matrix4x4 pixelCoordToViewDirWS; // should be at the end
            public Matrix4x4 viewProjectionNoCameraTrans;

            public Vector3 worldSpaceCameraPos;
            internal float pad0;

            public Vector3 worldSpaceCameraPosViewOffset;
            internal float pad1;

            public Vector3 prevWorldSpaceCameraPos;
            internal float pad2;
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct ReadableShaderVariablesXR
        {
            public fixed float _XRViewMatrix[32];
            public fixed float _XRInvViewMatrix[32];
            public fixed float _XRProjMatrix[32];
            public fixed float _XRInvProjMatrix[32];
            public fixed float _XRViewProjMatrix[32];
            public fixed float _XRInvViewProjMatrix[32];
            public fixed float _XRNonJitteredViewProjMatrix[32];
            public fixed float _XRPrevViewProjMatrix[32];
            public fixed float _XRPrevInvViewProjMatrix[32];
            public fixed float _XRPrevViewProjMatrixNoCameraTrans[32];
            public fixed float _XRViewProjMatrixNoCameraTrans[32];
            public fixed float _XRPixelCoordToViewDirWS[32];

            public fixed float _XRWorldSpaceCameraPos[8];
            public fixed float _XRWorldSpaceCameraPosViewOffset[8];
            public fixed float _XRPrevWorldSpaceCameraPos[8];
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct ReadableShaderVariablesGlobal
        {
            public Matrix4x4 _ViewMatrix;

            public Matrix4x4 _CameraViewMatrix;

            public Matrix4x4 _InvViewMatrix;

            public Matrix4x4 _ProjMatrix;

            public Matrix4x4 _InvProjMatrix;

            public Matrix4x4 _ViewProjMatrix;

            public Matrix4x4 _CameraViewProjMatrix;

            public Matrix4x4 _InvViewProjMatrix;

            public Matrix4x4 _NonJitteredViewProjMatrix;

            public Matrix4x4 _PrevViewProjMatrix;

            public Matrix4x4 _PrevInvViewProjMatrix;

            public Vector4 _WorldSpaceCameraPos_Internal;

            public Vector4 _PrevCamPosRWS_Internal;

            public Vector4 _ScreenSize;

            public Vector4 _PostProcessScreenSize;

            public Vector4 _RTHandleScale;

            public Vector4 _RTHandleScaleHistory;

            public Vector4 _RTHandlePostProcessScale;

            public Vector4 _RTHandlePostProcessScaleHistory;

            public Vector4 _DynamicResolutionFullscreenScale;

            public Vector4 _ZBufferParams;

            public Vector4 _ProjectionParams;

            public Vector4 unity_OrthoParams;

            public Vector4 _ScreenParams;

            public fixed float _FrustumPlanes[24];

            public Vector4 _TaaFrameInfo;

            public Vector4 _TaaJitterStrength;

            public Vector4 _Time;

            public Vector4 _SinTime;

            public Vector4 _CosTime;

            public Vector4 unity_DeltaTime;

            public Vector4 _TimeParameters;

            public Vector4 _LastTimeParameters;

            public int _FogEnabled;

            public int _PBRFogEnabled;

            public int _EnableVolumetricFog;

            public float _MaxFogDistance;

            public Vector4 _FogColor;

            public float _FogColorMode;

            public float _GlobalMipBias;

            public float _GlobalMipBiasPow2;

            public float _Pad0;

            public Vector4 _MipFogParameters;

            public Vector4 _HeightFogBaseScattering;

            public float _HeightFogBaseExtinction;

            public float _HeightFogBaseHeight;

            public float _GlobalFogAnisotropy;

            public int _VolumetricFilteringEnabled;

            public Vector2 _HeightFogExponents;

            public int _FogDirectionalOnly;

            public float _FogGIDimmer;

            public Vector4 _VBufferViewportSize;

            public Vector4 _VBufferLightingViewportScale;

            public Vector4 _VBufferLightingViewportLimit;

            public Vector4 _VBufferDistanceEncodingParams;

            public Vector4 _VBufferDistanceDecodingParams;

            public uint _VBufferSliceCount;

            public float _VBufferRcpSliceCount;

            public float _VBufferRcpInstancedViewCount;

            public float _VBufferLastSliceDist;

            public Vector4 _ShadowAtlasSize;

            public Vector4 _CascadeShadowAtlasSize;

            public Vector4 _AreaShadowAtlasSize;

            public Vector4 _CachedShadowAtlasSize;

            public Vector4 _CachedAreaShadowAtlasSize;

            public int _ReflectionsMode;

            public int _UnusedPadding0;

            public int _UnusedPadding1;

            public int _UnusedPadding2;

            public uint _DirectionalLightCount;

            public uint _PunctualLightCount;

            public uint _AreaLightCount;

            public uint _EnvLightCount;

            public int _EnvLightSkyEnabled;

            public uint _CascadeShadowCount;

            public int _DirectionalShadowIndex;

            public uint _EnableLightLayers;

            public uint _EnableSkyReflection;

            public uint _EnableSSRefraction;

            public float _SSRefractionInvScreenWeightDistance;

            public float _ColorPyramidLodCount;

            public float _DirectionalTransmissionMultiplier;

            public float _ProbeExposureScale;

            public float _ContactShadowOpacity;

            public float _ReplaceDiffuseForIndirect;

            public Vector4 _AmbientOcclusionParam;

            public float _IndirectDiffuseLightingMultiplier;

            public uint _IndirectDiffuseLightingLayers;

            public float _ReflectionLightingMultiplier;

            public uint _ReflectionLightingLayers;

            public float _MicroShadowOpacity;

            public uint _EnableProbeVolumes;

            public uint _ProbeVolumeCount;

            public float _SlopeScaleDepthBias;

            public Vector4 _CookieAtlasSize;

            public Vector4 _CookieAtlasData;

            public Vector4 _ReflectionAtlasCubeData;

            public Vector4 _ReflectionAtlasPlanarData;

            public uint _NumTileFtplX;

            public uint _NumTileFtplY;

            public float g_fClustScale;

            public float g_fClustBase;

            public float g_fNearPlane;

            public float g_fFarPlane;

            public int g_iLog2NumClusters;

            public uint g_isLogBaseBufferEnabled;

            public uint _NumTileClusteredX;

            public uint _NumTileClusteredY;

            public int _EnvSliceSize;

            public uint _EnableDecalLayers;

            public fixed float _ShapeParamsAndMaxScatterDists[64];

            public fixed float _TransmissionTintsAndFresnel0[64];

            public fixed float _WorldScalesAndFilterRadiiAndThicknessRemaps[64];

            public fixed float _DiffusionProfileHashTable[64];

            public uint _EnableSubsurfaceScattering;

            public uint _TexturingModeFlags;

            public uint _TransmissionFlags;

            public uint _DiffusionProfileCount;

            public Vector2 _DecalAtlasResolution;

            public uint _EnableDecals;

            public uint _DecalCount;

            public float _OffScreenDownsampleFactor;

            public uint _OffScreenRendering;

            public uint _XRViewCount;

            public int _FrameCount;

            public Vector4 _CoarseStencilBufferSize;

            public int _IndirectDiffuseMode;

            public int _EnableRayTracedReflections;

            public int _RaytracingFrameIndex;

            public uint _EnableRecursiveRayTracing;

            public int _TransparentCameraOnlyMotionVectors;

            public float _GlobalTessellationFactorMultiplier;

            public float _SpecularOcclusionBlend;

            public float _DeExposureMultiplier;

            public Vector4 _ScreenSizeOverride;

            public Vector4 _ScreenCoordScaleBias;
        }
    }
}
