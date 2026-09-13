using DunGen;
using HarmonyLib;
using LethalPerformance.Caching;
using LethalPerformance.Utilities;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace LethalPerformance.Patches.ReferenceHolder;

/// <summary>
/// Handles caching of moon stuff, like <see cref="ItemDropship"/> and <see cref="RuntimeDungeon"/>
/// </summary>
internal static class MoonCachingPatch
{
    // When adding new cached instance, clear the instance at the dereference method
    private static readonly UnsafeCachedInstance<ItemDropship> s_ItemDropship
        = UnsafeCacheManager.AddReferenceToMap(new ManualCachedInstance<ItemDropship>());
    internal static readonly UnsafeCachedInstance<RuntimeDungeon> s_RuntimeDungeon
        = UnsafeCacheManager.AddReferenceToMap(new ManualCachedInstance<RuntimeDungeon>());
    private static readonly UnsafeCachedInstance<animatedSun> s_AnimatedSun
        = UnsafeCacheManager.AddReferenceToMap(new ManualCachedInstance<animatedSun>());

    private static int s_LastCalledSceneId = -1;

    // TODO
    // SteamValveHazard(?)
    // EnemyAINestSpawnObject
    // RandomMapObject
    // SpawnSyncedObject(?)

    [HarmonyPatch(typeof(NavMeshSurface))]
    private static class Patch_NavMeshSurface
    {
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

            var dropship = FindDropship(scene);
            var dungeon = FindDungeon(scene);
            var sun = FindAnimatedSun(scene);

            LethalPerformancePlugin.Instance.Logger.LogInfo($"Found Dropship:{dropship}, Dungeon:{dungeon}, Sun:{sun}");

            // Making it a bit more robust if one of object is missing, then we still proccess
            if (dropship || dungeon || sun)
            {
                s_LastCalledSceneId = sceneHandle;
            }
        }

        private static bool FindDungeon(Scene scene)
        {
            RuntimeDungeon dungeon;

            var dungeonGeneratorObject = GameObject.Find("/Systems/LevelGeneration/DungeonGenerator");
            if (dungeonGeneratorObject != null && dungeonGeneratorObject.TryGetComponent(out dungeon))
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

            s_RuntimeDungeon.SetInstance(null);
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

            s_ItemDropship.SetInstance(null);
            return false;
        }

        private static readonly string[] s_AnimatedSunPaths =
        [
            "/Environment/Lighting/BrightDay/Sun/SunAnimContainer",
            "/Environment/Lighting/BrightDay/Sun/BlizzardSunAnimContainer", // Dine, Rend, Artifice
        ];

        private static bool FindAnimatedSun(Scene scene)
        {
            animatedSun sun;

            foreach (var path in s_AnimatedSunPaths)
            {
                var sunAnimContainer = GameObject.Find(path);
                if (sunAnimContainer != null && sunAnimContainer.TryGetComponent<animatedSun>(out sun))
                {
                    s_AnimatedSun.SetInstance(sun);
                    return true;
                }
            }

            using var _ = ListPool<GameObject>.Get(out var list);
            scene.GetRootGameObjects(list);

            foreach (var obj in list)
            {
                sun = obj.GetComponentInChildren<animatedSun>(includeInactive: false);
                if (sun == null)
                {
                    continue;
                }

                s_AnimatedSun.SetInstance(sun);
                return true;
            }

            s_AnimatedSun.SetInstance(null);
            return false;
        }
    }

    [HarmonyPatch(typeof(ElevatorAnimationEvents))]
    private static class Patch_ElevatorAnimationEvents
    {
        // Dereference code was moved from NavMeshSurface.OnDisable to this,
        // because of moons that doing some hacks to make enemies move on bridges(?).

        [HarmonyPatch(nameof(ElevatorAnimationEvents.ElevatorFullyRunning))]
        [HarmonyPrefix]
        private static void ElevatorFullyRunning()
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
            s_AnimatedSun.SetInstance(null);
        }
    }
}
