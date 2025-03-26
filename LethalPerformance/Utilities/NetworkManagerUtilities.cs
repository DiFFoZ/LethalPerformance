using System.Collections.Generic;
using Unity.Netcode;

namespace LethalPerformance.Utilities;
internal static class NetworkManagerUtilities
{
    public static void FindAllSpawnedNetworkBehaviour<T>(List<T> spawnedObjects) where T : Object
    {
        var manager = NetworkManager.Singleton;
        if (manager == null
            || manager.SpawnManager == null)
        {
            return;
        }

        foreach (var obj in manager.SpawnManager.SpawnedObjectsList)
        {
            var o = obj.GetComponentInParent<T>();
            if (o != null)
            {
                spawnedObjects.Add(o);
            }
        }
    }
}
