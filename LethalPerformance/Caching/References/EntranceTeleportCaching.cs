using LethalPerformance.Patcher.API;
using LethalPerformance.Utilities;
using UnityEngine;
using UnityEngine.Pool;

namespace LethalPerformance.Caching.References;
internal static class EntranceTeleportCaching
{
    [InitializeOnAwake]
    public static void Initialize()
    {
        UnsafeCacheManager.AddActionToMap(typeof(EntranceTeleport), GetEntranceTeleports);
    }

    private static InstancesResult GetEntranceTeleports(FindObjectsInactive inactive)
    {
        using var _ = ListPool<EntranceTeleport>.Get(out var list);
        NetworkManagerUtilities.FindAllSpawnedNetworkBehaviour(list);

        return InstancesResult.Found(list.ToArray());
    }
}
