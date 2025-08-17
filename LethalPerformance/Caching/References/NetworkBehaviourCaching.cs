using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using LethalPerformance.Patcher.API;
using LethalPerformance.Utilities;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

namespace LethalPerformance.Caching.References;
internal static class NetworkBehaviourCaching
{
    private static readonly HashSet<Type> s_TypesToCache = new()
    {
        typeof(VehicleController),
        typeof(GrabbableObject),
        typeof(DepositItemsDesk),
        typeof(ShipTeleporter),
        typeof(BreakerBox),
        typeof(MineshaftElevatorController),
        typeof(ShipLights),
        typeof(TVScript),
        typeof(EnemyVent),
        typeof(TerminalAccessibleObject),
        typeof(DoorLock),
        typeof(UnlockableSuit),
        typeof(Landmine),
        typeof(Turret),
        typeof(SpikeRoofTrap),
        typeof(StoryLog),
        typeof(EntranceTeleport)
        // commented, because it's not longer network behaviour
        //typeof(SteamValveHazard),
        // signal translator
        // enemy ai
    };

    private static readonly MethodInfo s_FindWithSpawnedBehaviours = typeof(NetworkBehaviourCaching)
            .GetMethod(nameof(FindWithSpawnedBehaviours), AccessTools.all);

    [InitializeOnAwake]
    internal static void Initialize()
    {
        // maybe add another actionToMap method that only allows 1 instance globally, so we don't allocate array

        foreach (var type in s_TypesToCache)
        {
#if ENABLE_PROFILER
            if (!typeof(NetworkBehaviour).IsAssignableFrom(type))
            {
                throw new Exception($"{type} is no longer behaviour");
            }
#endif

                AddActionToMap(type);
        }
    }

    private static void AddActionToMap(Type type)
    {
        var genericMethod = s_FindWithSpawnedBehaviours.MakeGenericMethod(type);

        var @delegate = (UnsafeCacheManager.TryGetInstances)
            Delegate.CreateDelegate(typeof(UnsafeCacheManager.TryGetInstances), genericMethod);

        UnsafeCacheManager.AddActionToMap(type, @delegate);
    }

    private static InstancesResult FindWithSpawnedBehaviours<T>(FindObjectsInactive inactive) where T : Object
    {
        if (inactive == FindObjectsInactive.Include)
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning($"{typeof(T).Name} search called with inactive objects, probably will cause incompatibility!");
        }

        using var _ = ListPool<T>.Get(out var list);

        NetworkManagerUtilities.FindAllSpawnedNetworkBehaviour(list);

        return InstancesResult.Found(list.ToArray() as Behaviour[]);
    }
}
