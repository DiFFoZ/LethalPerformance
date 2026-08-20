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
    // When adding new cached instance, clear the instance at the dereference method
    private static readonly UnsafeCachedInstance<ItemDropship> s_ItemDropship
        = UnsafeCacheManager.AddReferenceToMap(new ManualCachedInstance<ItemDropship>());
    internal static readonly UnsafeCachedInstance<RuntimeDungeon> s_RuntimeDungeon
        = UnsafeCacheManager.AddReferenceToMap(new ManualCachedInstance<RuntimeDungeon>());
    private static readonly UnsafeCachedInstance<animatedSun> s_AnimatedSun
        = UnsafeCacheManager.AddReferenceToMap(new ManualCachedInstance<animatedSun>());

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
                return;
            }

            var found = FindDropship(scene) && FindDungeon(scene) && FindAnimatedSun(scene);
            if (!found)
            {
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

            if (gameMixer == null || musicMixer == null)
            {
                return;
            }

            // Deleting AudioMixerSnapshot would crash the game when reloading a lobby.

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
            }
        }

        private static bool FindDungeon(Scene scene)
        {
            RuntimeDungeon dungeon;

            var dungeonGeneratorObject = GameObject.Find("/Systems/LevelGeneration/DungeonGenerator");
            if (dungeonGeneratorObject != null)
            {
                // moon on old version may use old dungen reference
                // so if it's not resolved then just set dungeon to null
                // and try to expect LLL to create it for us.
                dungeonGeneratorObject.TryGetComponent(out dungeon);
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
