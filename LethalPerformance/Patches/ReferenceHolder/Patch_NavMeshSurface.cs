#define UNITY_ASSERTIONS

using System;
using System.Collections.Generic;
using DunGen;
using HarmonyLib;
using LethalPerformance.Caching;
using LethalPerformance.Utilities;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace LethalPerformance.Patches.ReferenceHolder;

/// <summary>
/// Handles caching of moon stuff, like <see cref="ItemDropship"/> and <see cref="RuntimeDungeon"/>
/// </summary>
[HarmonyPatch(typeof(NavMeshSurface))]
internal static class Patch_NavMeshSurface
{
    private static int s_LastCalledSceneId = -1;

    private static readonly UnsafeCachedInstance<ItemDropship> s_ItemDropship
        = UnsafeCacheManager.AddReferenceToMap(new ManualCachedInstance<ItemDropship>());
    private static readonly UnsafeCachedInstance<RuntimeDungeon> s_RuntimeDungeon
        = UnsafeCacheManager.AddReferenceToMap(new ManualCachedInstance<RuntimeDungeon>());

    [HarmonyPatch("OnEnable")]
    [HarmonyPostfix]
    public static void OnEnable()
    {
        var scene = SceneUtilities.GetLastLoadedScene();
        if (!scene.IsValid())
        {
            return;
        }

        var sceneHandle = scene.handle;
        if (sceneHandle == s_LastCalledSceneId)
        {
            return;
        }

        s_LastCalledSceneId = sceneHandle;

        FindDropship(scene);
        FindDungeon(scene);
    }

    private static void FindDungeon(Scene scene)
    {
        var dungeonGeneratorObject = GameObject.Find("/Systems/LevelGeneration/DungeonGenerator");
        if (dungeonGeneratorObject != null && dungeonGeneratorObject.TryGetComponent<RuntimeDungeon>(out var dungeon))
        {
            s_RuntimeDungeon.SetInstance(dungeon);
            return;
        }

        using var _ = ListPool<GameObject>.Get(out var list);
        scene.GetRootGameObjects(list);

        foreach (var obj in list)
        {
            dungeon = obj.GetComponentInChildren<RuntimeDungeon>(includeInactive: false);
            if (dungeon == null)
            {
                continue;
            }

            s_RuntimeDungeon.SetInstance(dungeon);
            return;
        }
    }

    private static void FindDropship(Scene scene)
    {
        var itemShipObject = GameObject.Find("/Systems/ItemShipAnimContainer/ItemShip");
        if (itemShipObject != null && itemShipObject.TryGetComponent<ItemDropship>(out var dropship))
        {
            s_ItemDropship.SetInstance(dropship);
            return;
        }

        using var _ = ListPool<GameObject>.Get(out var list);
        scene.GetRootGameObjects(list);

        foreach (var obj in list)
        {
            dropship = obj.GetComponentInChildren<ItemDropship>(includeInactive: false);
            if (dropship == null)
            {
                continue;
            }

            s_ItemDropship.SetInstance(dropship);
            return;
        }
    }

    [HarmonyPatch("OnDisable")]
    [HarmonyPostfix]
    public static void OnDisable()
    {
        var scene = SceneUtilities.GetLastLoadedScene();
        if (!scene.IsValid())
        {
            return;
        }

        var sceneHandle = scene.handle;
        if (sceneHandle != s_LastCalledSceneId)
        {
            return;
        }

        s_LastCalledSceneId = -1;

        s_ItemDropship.SetInstance(null);
        s_RuntimeDungeon.SetInstance(null);
    }
}
