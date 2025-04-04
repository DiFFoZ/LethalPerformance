using System;
using System.Collections.Generic;
using LethalPerformance.Patcher.API;
using LethalPerformance.Utilities;
using Unity.Netcode;
using UnityEngine.Pool;

namespace LethalPerformance.Caching.References;
internal static class NetworkBehaviourCaching
{
    private static readonly HashSet<Type> s_TypesToCache = new()
    {
        typeof(EntranceTeleport),
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

    [InitializeOnAwake]
    internal static void Initialize()
    {
        // maybe add another actionToMap method that only allows 1 instance globally, so we don't allocate array

        foreach (var type in s_TypesToCache)
        {
            UnsafeCacheManager.AddActionToMap(type, (inactive) =>
            {
                if (inactive == UnityEngine.FindObjectsInactive.Include)
                {
                    LethalPerformancePlugin.Instance.Logger.LogWarning($"{type.Name} search called with inactive objects, probably will cause incompatibility!");
                }

                using var _ = ListPool<NetworkBehaviour>.Get(out var list);

                NetworkManagerUtilities.FindAllSpawnedNetworkBehaviour(type, list);

                return InstancesResult.Found(list.ToArray());
            });
        }
    }
}
