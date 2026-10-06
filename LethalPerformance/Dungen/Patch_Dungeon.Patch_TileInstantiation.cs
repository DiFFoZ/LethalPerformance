using DunGen;
using DunGen.Generation;
using HarmonyLib;
using LethalPerformance.Patcher.API;
using System;
using System.Collections;
using UnityEngine;

namespace LethalPerformance.Dungen;

internal static partial class Patch_Dungeon
{
    private static class Patch_TileInstantiation
    {
        private static readonly AccessTools.FieldRef<TileInstanceSource, TileInstanceSpawnedDelegate> s_TileInstanceSpawned =
            AccessTools.FieldRefAccess<TileInstanceSource, TileInstanceSpawnedDelegate>(nameof(TileInstanceSource.TileInstanceSpawned));

        [HarmonyCleanup]
        public static Exception? Cleanup(Exception exception)
        {
            return HarmonyExceptionHandler.ReportException(exception);
        }

        [HarmonyPatch(typeof(Dungeon), nameof(Dungeon.FromProxy))]
        [HarmonyPrefix]
        public static void FromProxyPrefix(DungeonProxy proxyDungeon, DungeonGenerator generator, ref Func<bool> shouldSkipFrame)
        {
            // causing InstantiateAsync to never complete.
            if (!generator.GenerateAsynchronously)
            {
                return;
            }

            if (!TileInstantiationAsync.Start(proxyDungeon.AllTiles, generator.Root.transform))
            {
                return;
            }

            var originalShouldSkipFrame = shouldSkipFrame;
            shouldSkipFrame = () => TileInstantiationAsync.HasIncompleteFront() || originalShouldSkipFrame();
        }

        [HarmonyPatch(typeof(Dungeon), nameof(Dungeon.FromProxy))]
        [HarmonyPostfix]
        public static void FromProxyPostfix(ref IEnumerator __result)
        {
            if (!TileInstantiationAsync.IsActive)
            {
                return;
            }

            __result = TileInstantiationAsync.PumpFromProxy(__result);
        }

        [HarmonyPatch(typeof(Dungeon), nameof(Dungeon.Clear), [])]
        [HarmonyPostfix]
        public static void DungeonCleared()
        {
            TileInstantiationAsync.StartSpawning();
        }

        [HarmonyPatch(typeof(TileInstanceSource), nameof(TileInstanceSource.SpawnTile))]
        [HarmonyPrefix]
        public static bool SpawnTile(TileInstanceSource __instance, Tile tilePrefab, Vector3 position, Quaternion rotation, ref Tile __result)
        {
            try
            {
                if (!TileInstantiationAsync.TryTake(tilePrefab, position, rotation, out var tile))
                {
                    LethalPerformancePlugin.Instance.Logger.LogWarning($"Tile async instantiation didn't returned the tile. Tile spawn {tilePrefab}\n{Environment.StackTrace}");
                    return true;
                }

                tile.RefreshTileEventReceivers();
                tile.TileSpawned();
                s_TileInstanceSpawned(__instance)?.Invoke(tilePrefab, tile, fromPool: false);

                __result = tile;
                return false;
            }
            catch (Exception e)
            {
                LethalPerformancePlugin.Instance.Logger.LogWarning(e.ToString());
            }
            return true;
        }

        [HarmonyPatch(typeof(DungeonGenerator), nameof(DungeonGenerator.Cancel))]
        [HarmonyPostfix]
        public static void Cancel()
        {
            TileInstantiationAsync.Reset();
        }
    }
}
