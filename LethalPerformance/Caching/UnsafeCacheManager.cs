﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Dissonance;
using UnityEngine;
using UnityEngine.Pool;

namespace LethalPerformance.Caching;
internal static class UnsafeCacheManager
{
    private delegate (bool, Behaviour?) TryGetInstance(FindObjectsInactive findObjectsInactive);
    private delegate (bool, Behaviour[]?) TryGetInstances(FindObjectsInactive findObjectsInactive);

    private static readonly Dictionary<Type, TryGetInstance> s_MapGettingInstance = new()
    {
        // checking for null for now, because Awake order execution is not defined properly
        // fixme: use patcher to reorder them https://docs.unity3d.com/2022.3/Documentation/ScriptReference/DefaultExecutionOrder.html
        [typeof(StartOfRound)] = (_) => (StartOfRound.Instance, StartOfRound.Instance),
        [typeof(GameNetworkManager)] = (_) => (true, GameNetworkManager.Instance),
        [typeof(HUDManager)] = (_) => (HUDManager.Instance, HUDManager.Instance),
        [typeof(GlobalEffects)] = (_) => (GlobalEffects.Instance, GlobalEffects.Instance),
        [typeof(IngamePlayerSettings)] = (_) => (true, IngamePlayerSettings.Instance),
        [typeof(SteamManager)] = (_) => (true, SteamManager.Instance),
        [typeof(VehicleController)] = (_) =>
        {
            if (StartOfRound.Instance != null && StartOfRound.Instance.attachedVehicle != null)
            {
                return (true, StartOfRound.Instance.attachedVehicle);
            }

            return (false, null);
        }
    };

    private static readonly Dictionary<Type, TryGetInstances> s_MapGettingInstances = new()
    {
        [typeof(PlayerVoiceIngameSettings)] = (inactive) =>
        {
            if (!TryGetCachedBehaviour<DissonanceComms>(inactive, out var comms))
            {
                return (false, null);
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

            return (true, voices.ToArray());
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
    }

    private static void AddReference<T>(string hierarchyPath) where T : Behaviour
    {
        var unsafeInstance = new AutoUnsafeCachedInstance<T>(hierarchyPath);
        s_MapGettingInstance[typeof(T)] = unsafeInstance.TryGetInstance;
    }

    public static UnsafeCachedInstance<T> AddReferenceToMap<T>(UnsafeCachedInstance<T> unsafeInstance) where T : Behaviour
    {
        s_MapGettingInstance[typeof(T)] = unsafeInstance.TryGetInstance;
        return unsafeInstance;
    }

    public static void CacheInstances()
    {
        foreach (var r in UnsafeCachedInstance.UnsafeCachedInstances)
        {
            if (r is IAutoInstance autoInstance)
            {
                autoInstance.SaveInstance();
            }
        }
    }

    public static bool TryGetCachedReference(Type type, FindObjectsInactive findObjectsInactive, out Object? cache)
    {
        if (s_MapGettingInstance.TryGetValue(type, out var cacheFunc))
        {
            (var isFound, Behaviour? cachedInstance) = cacheFunc(findObjectsInactive);
            if (isFound)
            {
                cache = cachedInstance;
                return true;
            }
        }

        cache = null;
        return false;
    }

    public static bool TryGetCachedReferences(Type type, FindObjectsInactive findObjectsInactive, out Object[]? cache)
    {
        if (s_MapGettingInstances.TryGetValue(type, out var cacheFunc))
        {
            (var isFound, Behaviour[]? cachedInstances) = cacheFunc(findObjectsInactive);
            if (isFound)
            {
                cache = cachedInstances;
                return true;
            }
        }

        if (s_MapGettingInstance.ContainsKey(type))
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning($"Woah! Someone requests to search of all {type.Name} objects, even if it's singleton");
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
        // do not Clear the list
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
