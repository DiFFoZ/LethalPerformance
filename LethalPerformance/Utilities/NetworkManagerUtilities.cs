using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

namespace LethalPerformance.Utilities;
internal static class NetworkManagerUtilities
{
    public static void FindAllSpawnedNetworkBehaviour<T>(List<T> spawnedObjects) where T : Object
    {
        FindAllSpawnedNetworkBehaviour(typeof(T), spawnedObjects);
    }

    public static void FindAllSpawnedNetworkBehaviour(Type type, IList spawnedObjects)
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

            var o = obj.GetComponentInChildren(type);
            if (o != null && ((NetworkBehaviour)o).NetworkObject == obj)
            {
                spawnedObjects.Add(o);
            }
        }
    }
}
