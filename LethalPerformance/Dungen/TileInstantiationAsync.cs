using System;
using System.Collections;
using System.Collections.Generic;
using DunGen;
using UnityEngine;

namespace LethalPerformance.Dungen;

internal static class TileInstantiationAsync
{
    private const int c_MaxInFlight = 4;

    private static readonly List<TileProxy> s_Tiles = new(256);
    private static readonly List<Tile> s_SpawnedTiles = new(256);
    private static readonly Queue<AsyncInstantiateOperation<Tile>> s_TileOperations = new(c_MaxInFlight);

    private static Transform? s_Parent;
    private static int s_NextTileIndex;
    private static bool s_Started;
    private static bool s_Finished;
    private static bool s_AwaitingClear;

    internal static bool IsActive => s_Started;

    public static void Skip()
    {
        if (s_Started)
        {
            Reset();
            return;
        }

        s_Finished = true;
    }

    public static bool Start(List<TileProxy> tiles, Transform parent)
    {
        if (s_Started || tiles == null || tiles.Count == 0 || parent == null)
        {
            return false;
        }

        s_Started = true;
        s_Finished = false;
        s_AwaitingClear = true;
        s_Parent = parent;
        s_NextTileIndex = 0;

        s_Tiles.Clear();
        s_Tiles.AddRange(tiles);
        s_SpawnedTiles.Clear();

        return true;
    }

    public static void Reset()
    {
        if (s_Finished)
        {
            return;
        }

        s_Finished = true;

        if (!s_Started)
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning("Tile instantiation async was not started");
        }

        while (s_TileOperations.Count > 0)
        {
            DestroyUnconsumed(s_TileOperations.Dequeue());
        }

        s_Tiles.Clear();
        s_SpawnedTiles.Clear();
        s_Parent = null;
        s_NextTileIndex = 0;
        s_AwaitingClear = false;
        s_Started = false;
    }

    internal static void ResetIfStarted()
    {
        if (!s_Started)
        {
            return;
        }

        Reset();
    }

    internal static void OnDungeonCleared()
    {
        if (!s_AwaitingClear)
        {
            return;
        }

        s_AwaitingClear = false;
        FillTileWindow();
    }

    internal static bool ShouldSkipUntilNextReady()
    {
        return HasIncompleteFront(s_TileOperations);
    }

    internal static IEnumerator PumpFromProxy(IEnumerator original)
    {
        try
        {
            while (original.MoveNext())
            {
                yield return original.Current;
                while (ShouldSkipUntilNextReady())
                {
                    yield return null;
                }
            }
        }
        finally
        {
            if (original is IDisposable disposable)
            {
                disposable.Dispose();
            }

            RestoreSiblingOrder();
        }
    }

    internal static bool TryTake(Tile prefab, Vector3 position, Quaternion rotation, out Tile tile)
    {
        _ = prefab;
        _ = position;
        _ = rotation;

        if (!TryTakeOperation(s_TileOperations, out tile))
        {
            return false;
        }

        s_SpawnedTiles.Add(tile);
        FillTileWindow();
        return true;
    }

    private static void FillTileWindow()
    {
        var parent = s_Parent;
        if (parent == null)
        {
            return;
        }

        var parameters = new InstantiateParameters
        {
            parent = parent,
            worldSpace = false,
        };

        while (s_TileOperations.Count < c_MaxInFlight && s_NextTileIndex < s_Tiles.Count)
        {
            var proxy = s_Tiles[s_NextTileIndex++];
            var placement = proxy.Placement;
            s_TileOperations.Enqueue(UnityEngine.Object.InstantiateAsync(proxy.PrefabTile, placement.Position, placement.Rotation, parameters));
        }
    }

    private static bool HasIncompleteFront<T>(Queue<AsyncInstantiateOperation<T>> operations) where T : UnityEngine.Object
    {
        return operations.Count > 0 && !operations.Peek().isDone;
    }

    private static bool TryTakeOperation<T>(Queue<AsyncInstantiateOperation<T>> operations, out T instance) where T : UnityEngine.Object
    {
        if (operations.Count == 0)
        {
            instance = null!;
            return false;
        }

        var operation = operations.Dequeue();
        if (!operation.isDone)
        {
            operation.WaitForCompletion();
        }

        var result = operation.Result;
        if (result == null || result.Length == 0 || result[0] == null)
        {
            instance = null!;
            return false;
        }

        instance = result[0];
        return true;
    }

    private static void DestroyUnconsumed<T>(AsyncInstantiateOperation<T> operation) where T : UnityEngine.Object
    {
        if (operation == null)
        {
            return;
        }

        if (!operation.isDone)
        {
            operation.Cancel();
        }

        var result = operation.Result;
        if (result == null)
        {
            return;
        }

        for (var i = 0; i < result.Length; i++)
        {
            var instance = result[i];
            if (instance == null)
            {
                continue;
            }

            var gameObject = instance as GameObject ?? (instance as Component)?.gameObject;
            if (gameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }

    private static void RestoreSiblingOrder()
    {
        // a hacky-hack to restore order of tiles as
        // we don't use allowSceneActivation toggle to
        // make spawning a lot faster

        for (var i = 0; i < s_SpawnedTiles.Count; i++)
        {
            var tile = s_SpawnedTiles[i];
            if (tile != null)
            {
                tile.transform.SetAsLastSibling();
            }
        }
    }
}
