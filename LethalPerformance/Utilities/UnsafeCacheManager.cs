﻿using System;
using System.Collections.Generic;
using Dissonance;
using UnityEngine;

namespace LethalPerformance.Utilities;
internal static class UnsafeCacheManager
{
    private static readonly UnsafeCachedInstance<Terminal> s_Terminal
        = new("/Environment/HangarShip/Terminal/TerminalTrigger/TerminalScript");
    private static readonly UnsafeCachedInstance<StartMatchLever> s_StartMatchLever
        = new("/Environment/HangarShip/StartGameLever");
    private static readonly UnsafeCachedInstance<MoldSpreadManager> s_MoldSpreadManager
        = new("/Systems/GameSystems/Misc/MoldSpread");
    private static readonly UnsafeCachedInstance<StormyWeather> s_StormyWeather
        = new("/Systems/GameSystems/TimeAndWeather/Stormy");
    private static readonly UnsafeCachedInstance<HangarShipDoor> s_HangarShipDoor
        = new("/Environment/HangarShip/AnimatedShipDoor");

    private delegate (bool, MonoBehaviour?) TryGetInstance(FindObjectsInactive findObjectsInactive);

    private static readonly Dictionary<Type, TryGetInstance> s_MapGettingInstance = new()
    {
        // checking for null for now, because Awake order execution is not defined properly
        // fixme: use patcher to reorder them https://docs.unity3d.com/2022.3/Documentation/ScriptReference/DefaultExecutionOrder.html
        [typeof(StartOfRound)] = (_) => (StartOfRound.Instance, StartOfRound.Instance),
        [typeof(TimeOfDay)] = (_) => (TimeOfDay.Instance, TimeOfDay.Instance),
        [typeof(GameNetworkManager)] = (_) => (GameNetworkManager.Instance, GameNetworkManager.Instance),
        [typeof(HUDManager)] = (_) => (HUDManager.Instance, HUDManager.Instance),
        [typeof(GlobalEffects)] = (_) => (GlobalEffects.Instance, GlobalEffects.Instance),
        [typeof(IngamePlayerSettings)] = (_) => (IngamePlayerSettings.Instance, IngamePlayerSettings.Instance),
        [typeof(RoundManager)] = (_) => (RoundManager.Instance, RoundManager.Instance),
        [typeof(ShipBuildModeManager)] = (_) => (ShipBuildModeManager.Instance, ShipBuildModeManager.Instance),
        [typeof(SoundManager)] = (_) => (SoundManager.Instance, SoundManager.Instance),
        [typeof(SteamManager)] = (_) => (SteamManager.Instance, SteamManager.Instance),
        // dissonance comms is also used in main menu, so checking for null here
        [typeof(DissonanceComms)] = (_) =>
        {
            if (StartOfRound.Instance != null && StartOfRound.Instance.voiceChatModule != null)
            {
                return (true, StartOfRound.Instance.voiceChatModule);
            }
            return (false, null);
        },
        // check the comment inside of TryGetOnlyActiveInstance
        [typeof(StormyWeather)] = s_StormyWeather.TryGetOnlyActiveInstance,
        [typeof(Terminal)] = s_Terminal.TryGetInstance,
        [typeof(StartMatchLever)] = s_StartMatchLever.TryGetInstance,
        [typeof(MoldSpreadManager)] = s_MoldSpreadManager.TryGetInstance,
        [typeof(HangarShipDoor)] = s_HangarShipDoor.TryGetInstance,
        [typeof(QuickMenuManager)] = (findObjectInactive) =>
        {
            if (GameNetworkManager.Instance == null
                || GameNetworkManager.Instance.localPlayerController == null
                || GameNetworkManager.Instance.localPlayerController.quickMenuManager == null)
            {
                return (false, null);
            }

            var menu = GameNetworkManager.Instance.localPlayerController.quickMenuManager;
            if (findObjectInactive is FindObjectsInactive.Exclude && !menu.isActiveAndEnabled)
            {
                return (true, null);
            }

            return (true, menu);
        }
    };

    public static void CacheInstances()
    {
        foreach (var r in UnsafeCachedInstance.UnsafeCachedInstances)
        {
            r.SaveInstance();
        }
    }

    public static bool TryGetCachedReference(Type type, FindObjectsInactive findObjectsInactive, out Object? cache)
    {
        if (s_MapGettingInstance.TryGetValue(type, out var cacheFunc))
        {
            (bool isFound, Behaviour? cachedInstance) = cacheFunc(findObjectsInactive);
            if (isFound)
            {
                cache = cachedInstance;
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
        // do not Clear the list
    }
}
