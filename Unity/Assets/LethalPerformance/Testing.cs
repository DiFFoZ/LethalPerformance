using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility;

namespace LethalPerformance.Unity
{
    [BurstCompile]
    public static unsafe class Testing
    {
        [BurstCompile]
        public static void Log(in FixedString128Bytes log)
        {
            Debug.Log(log);
        }

        [BurstCompile]
        public static unsafe void Test(in ReadableViewConstants* xrViewConstants, in int viewCount, ReadableShaderVariablesXR* cb)
        {
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
        }

        [BurstCompile]
        public static unsafe void UpdateShaderVariablesGlobalCB(ReadableShaderVariablesGlobal* cb,
            in FrameSettings frameSettings, in HDAdditionalCameraData.AntialiasingMode antialiasingMode, in CameraType cameraType,
            in ReadableViewConstants* mainViewConstants,
            in float4x4 vectorParams, in float4x4 vectorParams2, in float4x4 vectorParams3,
            in float4* frustumPlaneEquations, in float taaSharpenStrength, in int taaFrameIndex,
            in int colorPyramidHistoryMipCount, in float globalMipBias,
            in float time, in float lastTime, in int frameCount, in int viewCount,
            in float probeRangeCompressionFactor, in float deExposureMultiplier, in float4 screenCoordScaleBias,
            in bool useScreenSizeOverride, in float4 screenSizeOverride)
        {
            var isTaaEnabled = frameSettings.IsEnabled(FrameSettingsField.Postprocess)
                && antialiasingMode == HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing
                && cameraType == CameraType.Game;

            cb->_ViewMatrix = mainViewConstants->viewMatrix;
            cb->_CameraViewMatrix = mainViewConstants->viewMatrix;
            cb->_InvViewMatrix = mainViewConstants->invViewMatrix;
            cb->_ProjMatrix = mainViewConstants->projMatrix;
            cb->_InvProjMatrix = mainViewConstants->invProjMatrix;
            cb->_ViewProjMatrix = mainViewConstants->viewProjMatrix;
            cb->_CameraViewProjMatrix = mainViewConstants->viewProjMatrix;
            cb->_InvViewProjMatrix = mainViewConstants->invViewProjMatrix;
            cb->_NonJitteredViewProjMatrix = mainViewConstants->nonJitteredViewProjMatrix;
            cb->_PrevViewProjMatrix = mainViewConstants->prevViewProjMatrix;
            cb->_PrevInvViewProjMatrix = mainViewConstants->prevInvViewProjMatrix;
            cb->_WorldSpaceCameraPos_Internal = mainViewConstants->worldSpaceCameraPos;
            cb->_PrevCamPosRWS_Internal = mainViewConstants->prevWorldSpaceCameraPos;

            cb->_ScreenSize = vectorParams[0]; // screenSize
            cb->_PostProcessScreenSize = vectorParams[1]; // postProcessScreenSize
            cb->_RTHandleScale = vectorParams[2]; // RTHandles.rtHandleProperties.rtHandleScale
            cb->_RTHandleScaleHistory = vectorParams[3]; // m_HistoryRTSystem.rtHandleProperties.rtHandleScale
            cb->_RTHandlePostProcessScale = vectorParams2[0]; // m_PostProcessRTScales
            cb->_RTHandlePostProcessScaleHistory = vectorParams2[1]; // m_PostProcessRTScalesHistory
            cb->_DynamicResolutionFullscreenScale = new float4(vectorParams2[2][0] / vectorParams2[2][1], vectorParams2[2][2] / vectorParams2[2][3], 0f, 0f);

            cb->_ZBufferParams = vectorParams2[3]; // zBufferParams
            cb->_ProjectionParams = vectorParams3[0]; // projectionParams
            cb->unity_OrthoParams = vectorParams3[1]; // unity_OrthoParams
            cb->_ScreenParams = vectorParams3[2]; // screenParams

            MemCpy(cb->_FrustumPlanes, frustumPlaneEquations, sizeof(float4) * 6);

            cb->_TaaFrameInfo = new float4(taaSharpenStrength, 0f, taaFrameIndex, isTaaEnabled ? 1f : 0f);
            cb->_TaaJitterStrength = vectorParams3[3]; // taaJitter
            cb->_ColorPyramidLodCount = colorPyramidHistoryMipCount;
            cb->_GlobalMipBias = globalMipBias;
            cb->_GlobalMipBiasPow2 = (float)math.pow(2.0, globalMipBias);

            var deltaTime = Time.deltaTime;
            var smoothDeltaTime = Time.smoothDeltaTime;

            cb->_Time = new float4(time * 0.05f, time, time * 2f, time * 3f);
            cb->_SinTime = new float4(math.sin(time * 0.125f), math.sin(time * 0.25f), math.sin(time * 0.5f), math.sin(time));
            cb->_CosTime = new float4(math.cos(time * 0.125f), math.cos(time * 0.25f), math.cos(time * 0.5f), math.cos(time));
            cb->unity_DeltaTime = new float4(deltaTime, 1f / deltaTime, smoothDeltaTime, 1f / smoothDeltaTime);
            cb->_TimeParameters = new float4(time, math.sin(time), math.cos(time), 0f);
            cb->_LastTimeParameters = new float4(lastTime, math.sin(lastTime), math.cos(lastTime), 0f);
            cb->_FrameCount = frameCount;
            cb->_XRViewCount = (uint)viewCount;

            cb->_ProbeExposureScale = 1f / math.max(probeRangeCompressionFactor, 1E-06f);
            cb->_DeExposureMultiplier = deExposureMultiplier;
            cb->_TransparentCameraOnlyMotionVectors = (frameSettings.IsEnabled(FrameSettingsField.MotionVectors) && !frameSettings.IsEnabled(FrameSettingsField.TransparentsWriteMotionVector)) ? 1 : 0;
            cb->_ScreenCoordScaleBias = screenCoordScaleBias;
            cb->_ScreenSizeOverride = useScreenSizeOverride ? screenSizeOverride : cb->_ScreenSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe readonly struct ReadableViewConstants
        {
            public readonly Matrix4x4 viewMatrix;
            public readonly Matrix4x4 invViewMatrix;
            public readonly Matrix4x4 projMatrix;
            public readonly Matrix4x4 invProjMatrix;
            public readonly Matrix4x4 viewProjMatrix;
            public readonly Matrix4x4 invViewProjMatrix;
            public readonly Matrix4x4 nonJitteredViewProjMatrix;
            public readonly Matrix4x4 prevViewMatrix; // skipped
            public readonly Matrix4x4 prevViewProjMatrix;
            public readonly Matrix4x4 prevInvViewProjMatrix;
            public readonly Matrix4x4 prevViewProjMatrixNoCameraTrans;
            public readonly Matrix4x4 pixelCoordToViewDirWS; // should be at the end
            public readonly Matrix4x4 viewProjectionNoCameraTrans;

            public readonly Vector3 worldSpaceCameraPos;
            internal readonly float pad0;

            public readonly Vector3 worldSpaceCameraPosViewOffset;
            internal readonly float pad1;

            public readonly Vector3 prevWorldSpaceCameraPos;
            internal readonly float pad2;
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

            public fixed float _ShadowFrustumPlanes[24];

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
