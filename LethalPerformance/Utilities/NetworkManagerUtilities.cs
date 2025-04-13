using System;
using System.Collections;
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

            foreach (var behaviour in obj.ChildNetworkBehaviours)
            {
                if (behaviour is not T specificBehaviour)
                {
                    continue;
                }

                if (!behaviour.IsSpawned)
                {
                    continue;
                }

                if (behaviour.NetworkObject != obj)
                {
                    continue;
                }

                spawnedObjects.Add(specificBehaviour);
            }
        }
    }
}
