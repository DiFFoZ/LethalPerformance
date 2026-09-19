using System;
using System.Buffers;
using System.Buffers.Binary;
using DunGen;
using Unity.Netcode;

namespace LethalPerformance.Dungen;

internal static partial class Patch_Dungeon
{
    public static void PatchAll()
    {
        var harmony = LethalPerformancePlugin.Instance.Harmony!;

        harmony.PatchAll(typeof(Patch_Initialization));
        harmony.PatchAll(typeof(Patch_DoorwayPairFinder));
        harmony.PatchAll(typeof(Patch_Misc));
        harmony.PatchAll(typeof(Patch_TileInstantiation));

        DungeonGenerator.OnAnyDungeonGenerationComplete += DungeonGenerator_OnAnyDungeonGenerationComplete;
        DungeonGenerator.OnAnyDungeonGenerationStatusChanged += DungeonGenerator_OnAnyDungeonGenerationStatusChanged;
    }

    private static void DungeonGenerator_OnAnyDungeonGenerationStatusChanged(DungeonGenerator _, GenerationStatus status)
    {
        if (status == GenerationStatus.Failed)
        {
            TileInstantiationAsync.Reset();
        }
    }

    private static unsafe void DungeonGenerator_OnAnyDungeonGenerationComplete(DungeonGenerator generator)
    {
        var tiles = generator.CurrentDungeon.allTiles;

        var byteCount = tiles.Count * sizeof(int);
        var bytes = ArrayPool<byte>.Shared.Rent(byteCount);
        for (var i = 0; i < tiles.Count; i++)
        {
            var instanceId = tiles[i].Prefab.GetInstanceID();
            if (instanceId <= 0)
            {
                LethalPerformancePlugin.Instance.Logger.LogWarning("Not persistent asset");
                continue;
            }

            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i * sizeof(int)), instanceId);
        }

        uint hash;

        fixed (byte* b = bytes)
            hash = XXHash.Hash32(b, byteCount);

        ArrayPool<byte>.Shared.Return(bytes);

        LethalPerformancePlugin.Instance.Logger.LogMessage($"""

            -------------
            Dungeon generated
            Seed: {generator.Seed} ({generator.ChosenSeed})
            Tiles: {tiles.Count} ({hash:X8})
            -------------
            """);
        foreach (var (status, time) in generator.GenerationStats.GenerationStepTimes)
        {
            var str = string.Format("{0,-18} - {1}ms", status.ToString(), (int)time);
            LethalPerformancePlugin.Instance.Logger.LogMessage(str);
        }

        TileInstantiationAsync.Reset();
    }
}
