using System;
using DunGen;
using HarmonyLib;
using LethalPerformance.Caching;
using LethalPerformance.Extensions;
using LethalPerformance.Utilities;
using Unity.AI.Navigation;
using UnityEngine;
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
        LethalPerformancePlugin.Instance.Logger.LogDebug("Loaded new scene");
        if (!scene.IsValid())
        {
            return;
        }

        var sceneHandle = scene.handle;
        if (sceneHandle == s_LastCalledSceneId)
        {
            return;
        }

        if (scene.rootCount == 0)
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning("New scene loading triggered navmesh, but no roots on the scene! Mod initializing navmesh early?\n"
                + Environment.StackTrace);

            return;
        }

        var found = FindDropship(scene) && FindDungeon(scene);
        if (!found)
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning("New scene loading triggered navmesh, but nothing found! Mod initializing navmesh early?\n"
                + Environment.StackTrace);

            return;
        }

        s_LastCalledSceneId = sceneHandle;
    }

    private static bool FindDungeon(Scene scene)
    {
        var dungeonGeneratorObject = GameObject.Find("/Systems/LevelGeneration/DungeonGenerator");
        if (dungeonGeneratorObject != null && dungeonGeneratorObject.TryGetComponent<RuntimeDungeon>(out var dungeon))
        {
            s_RuntimeDungeon.SetInstance(dungeon);
            return true;
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
            return true;
        }

        return false;
    }

    private static bool FindDropship(Scene scene)
    {
        var itemShipObject = GameObject.Find("/Systems/ItemShipAnimContainer/ItemShip");
        if (itemShipObject != null && itemShipObject.TryGetComponent<ItemDropship>(out var dropship))
        {
            s_ItemDropship.SetInstance(dropship);
            return true;
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
            return true;
        }

        return false;
    }

    [HarmonyPatch("OnDisable")]
    [HarmonyPostfix]
    public static void OnDisable(NavMeshSurface __instance)
    {
        var scene = SceneUtilities.GetLastLoadedScene();
        LethalPerformancePlugin.Instance.Logger.LogDebug("Unloaded scene\n" + __instance.transform.GetScenePath());
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
