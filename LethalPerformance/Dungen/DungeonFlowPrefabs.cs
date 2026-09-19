using System.Collections.Generic;
using DunGen;
using DunGen.Graph;
using UnityEngine;

namespace LethalPerformance.Dungen;

internal static class DungeonFlowPrefabs
{
    private static readonly HashSet<TileSet> s_TileSets = [];
    private static readonly List<GameObject> s_Pending = new(256);
    private static readonly List<Doorway> s_Doorways = [];

    public static void Collect(DungeonFlow? dungeonFlow, HashSet<GameObject> prefabs)
    {
        if (dungeonFlow == null)
        {
            return;
        }

        s_TileSets.Clear();
        s_Pending.Clear();

        CollectTileSets(dungeonFlow);
        CollectTilePrefabs(prefabs);

        for (var i = 0; i < s_Pending.Count; i++)
        {
            CollectReferencedPrefabs(s_Pending[i], prefabs);
        }

        s_TileSets.Clear();
        s_Pending.Clear();
        s_Doorways.Clear();
    }

    private static void CollectTileSets(DungeonFlow dungeonFlow)
    {
        var nodes = dungeonFlow.Nodes;
        if (nodes != null)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                AddTileSets(nodes[i]?.TileSets);
            }
        }

        var lines = dungeonFlow.Lines;
        if (lines != null)
        {
            for (var i = 0; i < lines.Count; i++)
            {
                var archetypes = lines[i]?.DungeonArchetypes;
                if (archetypes == null)
                {
                    continue;
                }

                for (var j = 0; j < archetypes.Count; j++)
                {
                    var archetype = archetypes[j];
                    if (archetype == null)
                    {
                        continue;
                    }

                    AddTileSets(archetype.TileSets);
                    AddTileSets(archetype.BranchStartTileSets);
                    AddTileSets(archetype.BranchCapTileSets);
                }
            }
        }

        var injectionRules = dungeonFlow.TileInjectionRules;
        if (injectionRules == null)
        {
            return;
        }

        for (var i = 0; i < injectionRules.Count; i++)
        {
            var tileSet = injectionRules[i]?.TileSet;
            if (tileSet != null)
            {
                s_TileSets.Add(tileSet);
            }
        }
    }

    private static void CollectTilePrefabs(HashSet<GameObject> prefabs)
    {
        foreach (var tileSet in s_TileSets)
        {
            AddChanceTable(tileSet.TileWeights, prefabs);
        }
    }

    private static void CollectReferencedPrefabs(GameObject prefab, HashSet<GameObject> prefabs)
    {
        prefab.GetComponentsInChildren(true, s_Doorways);
        for (var i = 0; i < s_Doorways.Count; i++)
        {
            var doorway = s_Doorways[i];
            AddWeights(doorway.ConnectorPrefabWeights, prefabs);
            AddWeights(doorway.BlockerPrefabWeights, prefabs);

            // Add door component if still didn't have
            foreach (var door in doorway.ConnectorPrefabWeights)
            {
                if (!door.GameObject.TryGetComponent<Door>(out _))
                    door.GameObject.AddComponent<Door>();
            }
        }
    }

    private static void AddTileSets(List<TileSet>? tileSets)
    {
        if (tileSets == null)
        {
            return;
        }

        for (var i = 0; i < tileSets.Count; i++)
        {
            var tileSet = tileSets[i];
            if (tileSet != null)
            {
                s_TileSets.Add(tileSet);
            }
        }
    }

    private static void AddChanceTable(GameObjectChanceTable? table, HashSet<GameObject> prefabs)
    {
        var weights = table?.Weights;
        if (weights == null)
        {
            return;
        }

        for (var i = 0; i < weights.Count; i++)
        {
            TryAdd(weights[i]?.Value, prefabs);
        }
    }

    private static void AddWeights(List<GameObjectWeight>? weights, HashSet<GameObject> prefabs)
    {
        if (weights == null)
        {
            return;
        }

        for (var i = 0; i < weights.Count; i++)
        {
            TryAdd(weights[i]?.GameObject, prefabs);
        }
    }

    private static void TryAdd(GameObject? prefab, HashSet<GameObject> prefabs)
    {
        if (prefab == null)
        {
            return;
        }

        if (prefabs.Add(prefab))
        {
            s_Pending.Add(prefab);
        }
    }
}