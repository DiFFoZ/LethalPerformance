using System;
using System.Linq;
using DunGen;
using HarmonyLib;
using LethalPerformance.Audio;
using LethalPerformance.Caching;
using LethalPerformance.Utilities;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace LethalPerformance.Patches.ReferenceHolder;

/// <summary>
/// Handles caching of moon stuff, like <see cref="ItemDropship"/> and <see cref="RuntimeDungeon"/>
/// </summary>
internal static class MoonCachingPatch
{
    private static readonly UnsafeCachedInstance<ItemDropship> s_ItemDropship
        = UnsafeCacheManager.AddReferenceToMap(new ManualCachedInstance<ItemDropship>());
    private static readonly UnsafeCachedInstance<RuntimeDungeon> s_RuntimeDungeon
        = UnsafeCacheManager.AddReferenceToMap(new ManualCachedInstance<RuntimeDungeon>());

    private static int s_LastCalledSceneId = -1;

    // TODO
    // RandomScrapSpawn
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

            OptimizeAudioMixers();
        }

        private static void OptimizeAudioMixers()
        {
            // From what I understand not used audiomixer's from mods still processed with all effects,
            // and one of effect is pitch shifter that causing high processing time compared to other effects.
            // 
            // So finding all audio mixers and set bypass to reduce time processing.

            // TODO:
            // move it after generating dungeon (dawn lib is now hotloading dungeons)

            var gameMixer = SoundManager.Instance.diageticMixer;
            var musicMixer = GameObject.Find("/Systems/Audios/Music1").GetComponent<AudioSource>().outputAudioMixerGroup.audioMixer;

            var snapshots = Resources.FindObjectsOfTypeAll<AudioMixerSnapshot>().ToList();
            LethalPerformancePlugin.Instance.Logger.LogDebug("Found snaphots: " + snapshots.Count.ToString());

            foreach (var mixer in Resources.FindObjectsOfTypeAll<AudioMixer>())
            {
                if (mixer == gameMixer || mixer == musicMixer)
                {
                    LethalPerformancePlugin.Instance.Logger.LogDebug($"skipped real {mixer.name}");
                    continue;
                }

                var groups = mixer.FindMatchingGroups(string.Empty);
                foreach (var group in groups)
                {
                    var succ = UnityAudioMixerNative.SetEffectBypass(group, MixerEffect.PitchShifter, true);
                    var succ2 = UnityAudioMixerNative.SetEffectBypass(group, MixerEffect.Compressor, true);
                    var succ3 = UnityAudioMixerNative.SetEffectBypass(group, MixerEffect.Chorus, true);
                    //LethalPerformancePlugin.Instance.Logger.LogDebug($"{succ} {succ2} {succ3}");
                }

                // Removes snapshots that are from the mods (they are not used)
                foreach (var snapshot in snapshots.Where(s => s.audioMixer == mixer).ToList())
                {
                    Object.DestroyImmediate(snapshot, true);
                    snapshots.Remove(snapshot);
                }
            }

            // 5 in diegetic and 1 nondiegetic
            LethalPerformancePlugin.Instance.Logger.LogDebug("Left snaphots (should be 6): " + snapshots.Count.ToString());
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
        }
    }
}
