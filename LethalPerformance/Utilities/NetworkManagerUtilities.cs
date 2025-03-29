using System.Collections.Generic;
using Unity.Netcode;

namespace LethalPerformance.Utilities;
internal static class NetworkManagerUtilities
{
    public static void FindAllSpawnedNetworkBehaviour<T>(List<T> spawnedObjects) where T : Object
    {
        var manager = NetworkManager.Singleton;
        if (manager == null || manager.SpawnManager == null)
        {
            return;
        }

        foreach (var obj in manager.SpawnManager.SpawnedObjectsList)
        {
            if (obj == null)
            {
                continue;
            }

            var o = obj.GetComponentInChildren<T>();
            if (o != null)
            {
                spawnedObjects.Add(o);
            }
        }
    }
}
