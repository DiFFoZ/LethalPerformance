using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Dissonance;
using UnityEngine;
using UnityEngine.Pool;

namespace LethalPerformance.Caching;
internal static class UnsafeCacheManager
{
    public delegate InstanceResult TryGetInstance(FindObjectsInactive findObjectsInactive);
    public delegate InstancesResult TryGetInstances(FindObjectsInactive findObjectsInactive);

    private static readonly Dictionary<Type, TryGetInstance> s_MapGettingInstance = new()
    {
        [typeof(StartOfRound)] = (_) => new (StartOfRound.Instance, StartOfRound.Instance),
        [typeof(GameNetworkManager)] = (_) => InstanceResult.Found(GameNetworkManager.Instance),
        [typeof(HUDManager)] = (_) => new (HUDManager.Instance, HUDManager.Instance),
        [typeof(GlobalEffects)] = (_) => new (GlobalEffects.Instance, GlobalEffects.Instance),
        [typeof(IngamePlayerSettings)] = (_) => InstanceResult.Found(IngamePlayerSettings.Instance),
        [typeof(SteamManager)] = (_) => InstanceResult.Found(SteamManager.Instance)
    };

    private static readonly Dictionary<Type, TryGetInstances> s_MapGettingInstances = new()
    {
        [typeof(PlayerVoiceIngameSettings)] = (inactive) =>
        {
            if (!TryGetCachedBehaviour<DissonanceComms>(inactive, out var comms))
            {
                return InstancesResult.NotFound(null);
            }

            var activePlayers = comms._players._players;
            var pooledPlaybacks = comms._playbackPool._pool._items;

            using var _ = ListPool<PlayerVoiceIngameSettings>.Get(out var voices);
            for (var i = 0; i < activePlayers.Count; i++)
            {
                var playback = comms._players._players[i].Playback;
                if (playback is MonoBehaviour behaviour && behaviour != null
                    && behaviour.TryGetComponent<PlayerVoiceIngameSettings>(out var voice))
                {
                    voices.Add(voice);
                }
            }

            if (inactive is FindObjectsInactive.Include)
            {
                foreach (var playback in pooledPlaybacks)
                {
                    if (playback.TryGetComponent<PlayerVoiceIngameSettings>(out var voice))
                    {
                        voices.Add(voice);
                    }
                }
            }

            return InstancesResult.Found(voices.ToArray());
        }
    };

    static UnsafeCacheManager()
    {
        AddReference<DissonanceComms>("/Systems/DissonanceSetup");
        AddReference<RoundManager>("/Systems/GameSystems/RoundManager");
        AddReference<QuickMenuManager>("/Systems/GameSystems/QuickMenuManager");
        AddReference<TimeOfDay>("/Systems/GameSystems/TimeAndWeather");
        AddReference<SoundManager>("/Systems/GameSystems/SoundManager");
        AddReference<ShipBuildModeManager>("/Systems/GameSystems/ShipBuildMode");
        AddReference<MoldSpreadManager>("/Systems/GameSystems/Misc/MoldSpread");
        AddReference<StormyWeather>("/Systems/GameSystems/TimeAndWeather/Stormy");
        AddReference<BeltBagInventoryUI>("/Systems/UI/Canvas/IngamePlayerHUD/BeltBagUI");
        AddReference<AudioListener>("/Systems/Audios/PlayerAudioListener");

        AddReference<Terminal>("/Environment/HangarShip/Terminal/TerminalTrigger/TerminalScript");
        AddReference<StartMatchLever>("/Environment/HangarShip/StartGameLever");
        AddReference<HangarShipDoor>("/Environment/HangarShip/AnimatedShipDoor");

        static void AddReference<T>(string hierarchyPath) where T : Behaviour
        {
            var unsafeInstance = new AutoUnsafeCachedInstance<T>(hierarchyPath);
            s_MapGettingInstance[typeof(T)] = unsafeInstance.TryGetInstance;
        }
    }

    public static UnsafeCachedInstance<T> AddReferenceToMap<T>(UnsafeCachedInstance<T> unsafeInstance) where T : Behaviour
    {
        s_MapGettingInstance[typeof(T)] = unsafeInstance.TryGetInstance;
        return unsafeInstance;
    }

    public static void AddActionToMap(Type type, TryGetInstances action)
    {
        s_MapGettingInstances[type] = action;
    }

    public static void CacheInstances()
    {
        foreach (var uci in UnsafeCachedInstance.UnsafeCachedInstances)
        {
            if (uci is IAutoInstance autoInstance)
            {
                autoInstance.SaveInstance();
            }
        }
    }

    public static bool TryGetCachedReference(Type type, FindObjectsInactive findObjectsInactive, out Object? cache)
    {
        {
            if (s_MapGettingInstance.TryGetValue(type, out var cacheFunc))
            {
                var (isFound, cachedInstance) = cacheFunc(findObjectsInactive);
                if (isFound)
                {
                    cache = cachedInstance;
                    return true;
                }
            }
        }

        {
            if (s_MapGettingInstances.TryGetValue(type, out var cacheFunc))
            {
                var (isFound, cachedInstances) = cacheFunc(findObjectsInactive);
                if (isFound && cachedInstances!.Length > 0)
                {
                    cache = cachedInstances[0];
                }
                else
                {
                    cache = null;
                }

                return true;
            }
        }

        cache = null;
        return false;
    }

    public static bool TryGetCachedReferences(Type type, FindObjectsInactive findObjectsInactive, out Object[]? cache)
    {
        {
            if (s_MapGettingInstances.TryGetValue(type, out var cacheFunc))
            {
                var (isFound, cachedInstances) = cacheFunc(findObjectsInactive);
                if (isFound)
                {
                    cache = cachedInstances;
                    return true;
                }
            }
        }

        {
            if (s_MapGettingInstance.TryGetValue(type, out var cacheFunc))
            {
                var (isFound, cachedInstance) = cacheFunc(findObjectsInactive);
                if (isFound && cachedInstance != null)
                {
                    cache = [cachedInstance!];
                }
                else
                {
                    // we cannot return null
                    cache = [];
                }
                return true;
            }
        }

        cache = null;
        return false;
    }

    public static void CleanupCache()
    {
        foreach (var r in UnsafeCachedInstance.UnsafeCachedInstances)
        {
            r.Cleanup();
        }
    }

    private static bool TryGetCachedBehaviour<T>(FindObjectsInactive findObjectsInactive, [NotNullWhen(true)] out T? result) where T : Behaviour
    {
        if (TryGetCachedReference(typeof(T), findObjectsInactive, out var cache))
        {
            result = (T)cache!;
            return true;
        }

        result = null;
        return false;
    }
}
