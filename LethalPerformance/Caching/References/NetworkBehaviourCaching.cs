using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using LethalPerformance.Patcher.API;
using LethalPerformance.Utilities;
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
        typeof(SteamValveHazard),
        typeof(DoorLock),
        typeof(UnlockableSuit),
        typeof(Landmine),
        typeof(Turret),
        typeof(SpikeRoofTrap),
        typeof(StoryLog)
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
            AddActionToMap(type);
        }

        // see config description why it is separated

        LethalPerformancePlugin.Instance.Configuration.CacheEntranceTeleports.SettingChanged += CacheEntranceTeleports_SettingChanged;
        if (LethalPerformancePlugin.Instance.Configuration.CacheEntranceTeleports.Value)
        {
            CacheEntranceTeleports_SettingChanged(null!, null!);
        }
    }

    private static void CacheEntranceTeleports_SettingChanged(object sender, EventArgs e)
    {
        UnsafeCacheManager.RemoveActionToMap(typeof(EntranceTeleport));
        AddActionToMap(typeof(EntranceTeleport));
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
