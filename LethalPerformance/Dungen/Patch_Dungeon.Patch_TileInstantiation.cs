using System;
using System.Collections;
using DunGen;
using DunGen.Generation;
using HarmonyLib;
using LethalPerformance.Patcher.API;
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

        [HarmonyPatch(typeof(Dungeon), nameof(Dungeon.FromProxy), [typeof(DungeonProxy), typeof(DungeonGenerator), typeof(Func<bool>)])]
        [HarmonyPrefix]
        public static void FromProxyPrefix(DungeonProxy proxyDungeon, DungeonGenerator generator, ref Func<bool> shouldSkipFrame)
        {
            // causing InstantiateAsync to never complete.
            if (!generator.GenerateAsynchronously)
            {
                TileInstantiationAsync.Skip();
                return;
            }

            if (!TileInstantiationAsync.Start(proxyDungeon.AllTiles, generator.Root.transform))
            {
                return;
            }

            shouldSkipFrame = TileInstantiationAsync.ShouldSkipUntilNextReady;
        }

        [HarmonyPatch(typeof(Dungeon), nameof(Dungeon.FromProxy), [typeof(DungeonProxy), typeof(DungeonGenerator), typeof(Func<bool>)])]
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
            TileInstantiationAsync.OnDungeonCleared();
        }

        [HarmonyPatch(typeof(TileInstanceSource), nameof(TileInstanceSource.SpawnTile))]
        [HarmonyPrefix]
        public static bool SpawnTile(TileInstanceSource __instance, Tile tilePrefab, Vector3 position, Quaternion rotation, ref Tile __result)
        {
            if (!TileInstantiationAsync.TryTake(tilePrefab, position, rotation, out var tile))
            {
                return true;
            }

            // logic from original dungeon
            // todo: check if it really needed
            if (tile.TryGetComponent<Tile>(out var component))
            {
                component.RefreshTileEventReceivers();
                component.TileSpawned();
                s_TileInstanceSpawned(__instance)?.Invoke(tilePrefab, component, fromPool: false);
            }

            __result = tile;
            return false;
        }

        [HarmonyPatch(typeof(DungeonGenerator), nameof(DungeonGenerator.Cancel))]
        [HarmonyPostfix]
        public static void Cancel()
        {
            TileInstantiationAsync.ResetIfStarted();
        }
    }
}
