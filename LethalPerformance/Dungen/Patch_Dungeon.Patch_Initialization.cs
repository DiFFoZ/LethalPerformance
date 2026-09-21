using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DunGen;
using HarmonyLib;
using LethalLevelLoader;
using LethalPerformance.Patcher.API;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace LethalPerformance.Dungen;

internal static partial class Patch_Dungeon
{
    private static class Patch_Initialization
    {
        private static readonly HashSet<GameObject> s_Prefabs = [];
        private static readonly List<ProBuilderMesh> s_Meshes = [];
        private static readonly Dictionary<GameObject, TileProxy> s_TileProxies = new(256);

        [HarmonyCleanup]
        public static Exception? Cleanup(Exception exception)
        {
            return HarmonyExceptionHandler.ReportException(exception);
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void Start()
        {
            Patch_TileInstantiation.Cancel();

            s_Prefabs.Clear();
            s_TileProxies.Clear();

            if (Dependencies.IsModLoaded(Dependencies.DungenPlus))
            {
                Patch_DungeonPlus.AllowReset = true;
                Patch_DungeonPlus.ResetDictionary();
                Patch_DungeonPlus.AllowReset = false;
            }

            var extendedFlows = PatchedContent.ExtendedDungeonFlows;
            foreach (var flow in extendedFlows)
            {
                if (flow.ContentType is ContentType.External)
                {
                    continue;
                }

                DungeonFlowPrefabs.Collect(flow?.DungeonFlow, s_Prefabs);
            }

            foreach (var prefab in s_Prefabs)
            {
                StripProBuilder(prefab);
                CacheTileProxy(prefab);
            }

            s_Prefabs.Clear();
            s_Meshes.Clear();
        }

        [HarmonyPatch(typeof(DungeonGenerator), nameof(DungeonGenerator.Generate))]
        [HarmonyPrefix]
        public static void ApplySettings(DungeonGenerator __instance)
        {
            __instance.TriggerPlacement = TriggerPlacementMode.None;
        }

        [HarmonyPatch(typeof(DungeonGenerator), nameof(DungeonGenerator.PreProcess))]
        [HarmonyPostfix]
        public static void PrepandTileProxies(DungeonGenerator __instance)
        {
            // Dawn interior. Our TileProxies will be invalid here, so let dungen to generate them instead
            if (DungeonManager.CurrentExtendedDungeonFlow is { ContentType: ContentType.External })
            {
                return;
            }

            var preProcessData = __instance.preProcessData;
            foreach (var pair in s_TileProxies)
            {
                preProcessData.TryAdd(pair.Key, pair.Value);
            }
        }

        private static void CacheTileProxy(GameObject prefab)
        {
            if (prefab == null || s_TileProxies.ContainsKey(prefab) || !prefab.TryGetComponent<Tile>(out _))
            {
                return;
            }

            s_TileProxies.Add(prefab, new TileProxy(prefab, null));
        }

        private static void StripProBuilder(GameObject prefab)
        {
            prefab.GetComponentsInChildren(true, s_Meshes);
            for (var i = 0; i < s_Meshes.Count; i++)
            {
                var pb = s_Meshes[i];
                if (pb == null)
                {
                    continue;
                }

                if (pb.meshSyncState is MeshSyncState.Null or MeshSyncState.NeedsRebuild
                    && pb.vertexCount > 0 && pb.faceCount > 0)
                {
                    pb.ToMesh();
                    pb.Refresh();
                }

                pb.preserveMeshAssetOnDestroy = true;
                UnityEngine.Object.DestroyImmediate(pb, true);
            }
        }
    }
}
