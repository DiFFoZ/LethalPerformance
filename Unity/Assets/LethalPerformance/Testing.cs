using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
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
    }
}
