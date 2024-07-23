using System;
using System.Collections.Generic;
using Dissonance;
using UnityEngine;

namespace LethalPerformance.Utilities;
internal static class UnsafeCacheManager
{
    private delegate (bool, MonoBehaviour?) TryGetInstance(FindObjectsInactive findObjectsInactive);

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

    static UnsafeCacheManager()
    {
        AddReference<RoundManager>("/Systems/GameSystems/RoundManager", false);
        AddReference<QuickMenuManager>("/Systems/GameSystems/QuickMenuManager", false);
        AddReference<TimeOfDay>("/Systems/GameSystems/TimeAndWeather", false);
        AddReference<SoundManager>("/Systems/GameSystems/SoundManager", false);
        AddReference<ShipBuildModeManager>("/Systems/GameSystems/ShipBuildMode", false);
        AddReference<MoldSpreadManager>("/Systems/GameSystems/Misc/MoldSpread", false);

        AddReference<Terminal>("/Environment/HangarShip/Terminal/TerminalTrigger/TerminalScript", false);
        AddReference<StartMatchLever>("/Environment/HangarShip/StartGameLever", false);
        AddReference<HangarShipDoor>("/Environment/HangarShip/AnimatedShipDoor", false);

        AddReference<StormyWeather>("/Systems/GameSystems/TimeAndWeather/Stormy", true);
    }

    private static void AddReference<T>(string hierarchyPath, bool addCheckIfActive) where T : MonoBehaviour
    {
        var unsafeInstance = new UnsafeCachedInstance<T>(hierarchyPath);
        if (addCheckIfActive)
        {
            s_MapGettingInstance[typeof(T)] = unsafeInstance.TryGetOnlyActiveInstance;
        }
        else
        {
            s_MapGettingInstance[typeof(T)] = unsafeInstance.TryGetInstance;
        }
    }

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
