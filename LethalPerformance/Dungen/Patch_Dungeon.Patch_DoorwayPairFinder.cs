using System;
using System.Collections.Generic;
using System.Linq;
using DunGen;
using HarmonyLib;
using LethalPerformance.Patcher.API;
using UnityEngine;

namespace LethalPerformance.Dungen;

internal static partial class Patch_Dungeon
{
    private static class Patch_DoorwayPairFinder
    {
        private static readonly Dictionary<GameObjectChance, int> s_TileOrderIndex = new();
        private static readonly List<DoorwayPair> s_Pairs = new(256);
        private static readonly Comparison<DoorwayPair> s_ComparePairs = static (a, b) =>
        {
            var cmp = b.TileWeight.CompareTo(a.TileWeight);
            return cmp != 0 ? cmp : b.DoorwayWeight.CompareTo(a.DoorwayWeight);
        };

        private static bool IsTileAllowed(DoorwayPairFinder instance, TileProxy previousTile, TileProxy potentialNextTile, ref float weight)
        {
            if (instance.GetTileTemplateDelegate?.Target is not DungeonGenerator generator)
            {
                return instance.IsTileAllowedPredicate == null
                    || instance.IsTileAllowedPredicate(previousTile, potentialNextTile, ref weight);
            }

            var prefab = potentialNextTile.Prefab;
            var repeatsPrevious = previousTile != null && prefab == previousTile.Prefab;

            var repeatMode = TileRepeatMode.Allow;
            if (generator.OverrideRepeatMode)
            {
                repeatMode = generator.RepeatMode;
            }
            else if (potentialNextTile != null)
            {
                repeatMode = potentialNextTile.PrefabTile.RepeatMode;
            }

            switch (repeatMode)
            {
                case TileRepeatMode.Allow:
                    return true;

                case TileRepeatMode.DisallowImmediate:
                    return !repeatsPrevious;

                case TileRepeatMode.Disallow:
                    foreach (var proxy in generator.proxyDungeon.AllTiles)
                    {
                        if (proxy.Prefab == prefab)
                        {
                            return false;
                        }
                    }

                    return true;

                default:
                    throw new NotImplementedException($"TileRepeatMode {repeatMode} is not implemented");
            }
        }

        [HarmonyCleanup]
        public static Exception? Cleanup(Exception exception)
        {
            return HarmonyExceptionHandler.ReportException(exception);
        }

        [HarmonyPatch(typeof(DoorwayPairFinder), nameof(DoorwayPairFinder.GetDoorwayPairs))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        public static bool GetDoorwayPairsPatch(DoorwayPairFinder __instance, int? maxCount, bool __runOriginal, ref Queue<DoorwayPair> __result)
        {
            if (!__runOriginal)
            {
                return false;
            }

            __instance.tileOrder = __instance.CalculateOrderedListOfTiles();
            __instance.shouldStraightenNextConnection = __instance.CalculateShouldStraightenNextConnection();
            if (__instance.shouldStraightenNextConnection)
            {
                __instance.currentPathDirection = __instance.CalculateCurrentPathDirection();
            }
            if (__instance.currentPathDirection == null)
            {
                __instance.shouldStraightenNextConnection = false;
            }

            s_TileOrderIndex.Clear();

            FillTileOrderIndex(__instance);

            var pairsTile = __instance.PreviousTile == null
                ? __instance.GetPotentialDoorwayPairsForFirstTile()
                : __instance.GetPotentialDoorwayPairsForNonFirstTile();
            var pairs = pairsTile as List<DoorwayPair> ?? pairsTile.ToList();

            var take = pairs.Count;
            if (maxCount != null)
            {
                take = Mathf.Min(take, maxCount.Value);
            }

            if (pairs.Count > 1)
            {
                pairs.Sort(s_ComparePairs);
            }

            var queue = new Queue<DoorwayPair>(take);
            for (var i = 0; i < take; i++)
            {
                queue.Enqueue(pairs[i]);
            }
            __result = queue;

            return false;
        }

        [HarmonyPatch(typeof(DoorwayPairFinder), nameof(DoorwayPairFinder.CalculateOrderedListOfTiles))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        public static bool CalculateOrderedListOfTiles(DoorwayPairFinder __instance, bool __runOriginal, ref List<GameObjectChance> __result)
        {
            if (!__runOriginal)
            {
                return false;
            }

            var tileWeights = __instance.TileWeights;
            var list = new List<GameObjectChance>(tileWeights.Count);
            var table = new GameObjectChanceTable();
            table.Weights.AddRange(tileWeights);

            var weights = table.Weights;
            var isOnMainPath = __instance.IsOnMainPath;
            var normalizedDepth = __instance.NormalizedDepth;
            var random = __instance.RandomStream;

            while (HasAnyPositiveWeight(weights, isOnMainPath, normalizedDepth))
            {
                list.Add(table.GetRandom(random, isOnMainPath, normalizedDepth, null, allowImmediateRepeats: true, removeFromTable: true));
            }

            __result = list;
            return false;
        }

        [HarmonyPatch(typeof(DoorwayPairFinder), nameof(DoorwayPairFinder.GetPotentialDoorwayPairsForFirstTile))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        public static bool GetPotentialDoorwayPairsForFirstTile(DoorwayPairFinder __instance, bool __runOriginal, ref IEnumerable<DoorwayPair> __result)
        {
            if (!__runOriginal)
            {
                return false;
            }

            s_Pairs.Clear();
            FillTileOrderIndex(__instance);
            try
            {
                CollectFirstTilePairs(__instance, s_Pairs);
            }
            finally
            {
                s_TileOrderIndex.Clear();
            }

            __result = s_Pairs;
            return false;
        }

        [HarmonyPatch(typeof(DoorwayPairFinder), nameof(DoorwayPairFinder.GetPotentialDoorwayPairsForNonFirstTile))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        public static bool GetPotentialDoorwayPairsForNonFirstTile(DoorwayPairFinder __instance, bool __runOriginal, ref IEnumerable<DoorwayPair> __result)
        {
            if (!__runOriginal)
            {
                return false;
            }

            if (Dependencies.IsModLoaded(Dependencies.DungenPlus) && Patch_DungeonPlus.IsDunGenPlusActive())
            {
                // DunGen+ replaces vanilla search in postfix anyway

                __result = s_Pairs;
                return false;
            }

            s_Pairs.Clear();
            FillTileOrderIndex(__instance);
            try
            {
                CollectNonFirstTilePairs(__instance, s_Pairs);
            }
            finally
            {
                s_TileOrderIndex.Clear();
            }

            __result = s_Pairs;
            return false;
        }

        private static bool HasAnyPositiveWeight(List<GameObjectChance> weights, bool isOnMainPath, float normalizedDepth)
        {
            for (var i = 0; i < weights.Count; i++)
            {
                var chance = weights[i];
                if (chance.Value != null && chance.GetWeight(isOnMainPath, normalizedDepth) > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        private static void FillTileOrderIndex(DoorwayPairFinder instance)
        {
            if (s_TileOrderIndex.Count > 0)
            {
                return;
            }

            var tileOrder = instance.tileOrder;
            for (var i = 0; i < tileOrder.Count; i++)
            {
                var tile = tileOrder[i];
                if (tile != null && !s_TileOrderIndex.ContainsKey(tile))
                {
                    s_TileOrderIndex[tile] = i;
                }
            }
        }

        private static void CollectFirstTilePairs(DoorwayPairFinder instance, List<DoorwayPair> pairs)
        {
            foreach (var tileWeight in instance.TileWeights)
            {
                if (tileWeight == null || !s_TileOrderIndex.ContainsKey(tileWeight))
                {
                    continue;
                }

                var nextTile = instance.GetTileTemplateDelegate(tileWeight.Value);
                var weight = tileWeight.GetWeight(instance.IsOnMainPath, instance.NormalizedDepth) * (float)instance.RandomStream.NextDouble();
                if (!IsTileAllowed(instance, instance.PreviousTile, nextTile, ref weight))
                {
                    continue;
                }

                var doorways = nextTile.doorways;
                var tileSet = tileWeight.TileSet;
                foreach (var doorway in doorways)
                {
                    var doorwayWeight = instance.CalculateConnectionWeight(
                        new ProposedConnection(instance.DungeonProxy, null, nextTile, null, doorway));
                    pairs.Add(new DoorwayPair(null, null, nextTile, doorway, tileSet, weight, doorwayWeight));
                }
            }
        }

        private static void CollectNonFirstTilePairs(DoorwayPairFinder instance, List<DoorwayPair> pairs)
        {
            var previousTile = instance.PreviousTile;
            var previousDoorways = previousTile.doorways;
            var previousExits = previousTile.Exits;

            var hasUnusedExit = false;
            foreach (var exit in previousExits)
            {
                if (!exit.Used)
                {
                    hasUnusedExit = true;
                    break;
                }
            }

            foreach (var previousDoor in previousDoorways)
            {
                if (previousDoor.Used || previousDoor.IsDisabled)
                {
                    continue;
                }

                if (hasUnusedExit && !previousExits.Contains(previousDoor))
                {
                    continue;
                }

                foreach (var tileWeight in instance.TileWeights)
                {
                    if (tileWeight == null || !s_TileOrderIndex.TryGetValue(tileWeight, out var tileIndex))
                    {
                        continue;
                    }

                    var nextTile = instance.GetTileTemplateDelegate(tileWeight.Value);
                    float weight = instance.tileOrder.Count - tileIndex;
                    if (!IsTileAllowed(instance, previousTile, nextTile, ref weight))
                    {
                        continue;
                    }

                    var nextDoorways = nextTile.doorways;
                    var nextEntrances = nextTile.Entrances;
                    var nextExits = nextTile.Exits;
                    var requireEntrance = nextEntrances.Count > 0;
                    var blockSoleExit = nextExits.Count == 1;
                    var tileSet = tileWeight.TileSet;

                    foreach (var doorway in nextDoorways)
                    {
                        if (requireEntrance && !nextEntrances.Contains(doorway))
                        {
                            continue;
                        }

                        if (blockSoleExit && nextExits.Contains(doorway))
                        {
                            continue;
                        }

                        var doorwayWeight = 0f;
                        if (instance.IsValidDoorwayPairing(previousDoor, doorway, previousTile, nextTile, ref doorwayWeight))
                        {
                            pairs.Add(new DoorwayPair(previousTile, previousDoor, nextTile, doorway, tileSet, weight, doorwayWeight));
                        }
                    }
                }
            }
        }
    }
}
