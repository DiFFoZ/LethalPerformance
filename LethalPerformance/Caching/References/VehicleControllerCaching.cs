using LethalPerformance.Patcher.API;
using LethalPerformance.Utilities;
using UnityEngine;
using UnityEngine.Pool;

namespace LethalPerformance.Caching.References;
internal static class VehicleControllerCaching
{
    [InitializeOnAwake]
    public static void Initialize()
    {
        UnsafeCacheManager.AddActionToMap(typeof(VehicleController), FindVehicles);
    }

    private static InstancesResult FindVehicles(FindObjectsInactive findObjectsInactive)
    {
        using var _ = ListPool<VehicleController>.Get(out var list);
        NetworkManagerUtilities.FindAllSpawnedNetworkBehaviour(list);

        return InstancesResult.Found(list.ToArray());
    }
}
