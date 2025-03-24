using DunGen;
using HarmonyLib;
using LethalPerformance.Caching;
using LethalPerformance.Utilities;
using Unity.AI.Navigation;
using UnityEngine;

namespace LethalPerformance.Patches.ReferenceHolder;
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

        var itemShipObject = GameObject.Find("/Systems/ItemShipAnimContainer/ItemShip");
        if (itemShipObject != null && itemShipObject.TryGetComponent<ItemDropship>(out var dropship))
        {
            s_ItemDropship.SetInstance(dropship);
        }

        var dungeonGeneratorObject = GameObject.Find("/Systems/LevelGeneration/DungeonGenerator");
        if (dungeonGeneratorObject != null && dungeonGeneratorObject.TryGetComponent<RuntimeDungeon>(out var dungeon))
        {
            s_RuntimeDungeon.SetInstance(dungeon);
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
