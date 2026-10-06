using DunGen;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LethalPerformance.Dungen;

internal static class TileInstantiationAsync
{
    private const int c_MaxInFlight = 4;

    private static readonly List<TileProxy> s_Tiles = new(256);
    private static readonly List<Tile> s_SpawnedTiles = new(256);
    private static readonly Queue<TileSpawn> s_TileOperations = new(c_MaxInFlight);

    private static Transform? s_Parent;
    private static int s_NextTileIndex;
    private static bool s_Started;
    private static bool s_Finished;

    internal static bool IsActive => s_Started;

    public static bool Start(List<TileProxy> tiles, Transform parent)
    {
        if (s_Started)
        {
            return false;
        }

        s_Started = true;
        s_Finished = false;
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
            DestroyUnconsumed(s_TileOperations.Dequeue().Operation);
        }

        s_Tiles.Clear();
        s_SpawnedTiles.Clear();
        s_Parent = null;
        s_NextTileIndex = 0;
        s_Started = false;
    }

    public static bool HasIncompleteFront()
    {
        return s_TileOperations.Count > 0 && !s_TileOperations.Peek().Operation.isDone;
    }

    public static void StartSpawning()
    {
        FillTileWindow();
    }

    internal static IEnumerator PumpFromProxy(IEnumerator original)
    {
        try
        {
            while (HasIncompleteFront())
            {
                yield return null;
            }

            while (original.MoveNext())
            {
                yield return original.Current;
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
        if (s_TileOperations.Count == 0)
        {
            tile = null!;
            return false;
        }

        var spawn = s_TileOperations.Dequeue();
        var operation = spawn.Operation;
        if (!operation.isDone)
        {
            operation.WaitForCompletion();
        }

        var result = operation.Result;
        if (result == null || result.Length == 0 || result[0] == null)
        {
            tile = null!;
            return false;
        }

        tile = result[0];
        s_SpawnedTiles.Add(tile);
        FillTileWindow();

        return true;
    }

    private static void FillTileWindow()
    {
        var parameters = new InstantiateParameters
        {
            parent = s_Parent,
            worldSpace = false,
        };

        while (s_TileOperations.Count < c_MaxInFlight && s_NextTileIndex < s_Tiles.Count)
        {
            var proxy = s_Tiles[s_NextTileIndex++];
            var placement = proxy.Placement;

            s_TileOperations.Enqueue(new()
            {
                Operation = Object.InstantiateAsync(proxy.PrefabTile, placement.Position, placement.Rotation, parameters),
                Prefab = proxy.Prefab,
            });
        }
    }

    private static void DestroyUnconsumed(AsyncInstantiateOperation<Tile> operation)
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

            UnityEngine.Object.DestroyImmediate(instance.gameObject);
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

    private class TileSpawn
    {
        public AsyncInstantiateOperation<Tile> Operation { get; set; } = null!;
        public GameObject Prefab { get; set; } = null!;
    }
}
