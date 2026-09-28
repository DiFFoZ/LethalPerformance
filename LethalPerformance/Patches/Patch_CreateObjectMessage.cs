using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.Netcode;

namespace LethalPerformance.Patches;

[HarmonyPatch(typeof(CreateObjectMessage))]
internal static class Patch_CreateObjectMessage
{
    // A little compact with in future mod -- idk
    private const int c_Priority = Priority.Normal - 1;

    private static readonly List<int> s_Indicies = new();

    [HarmonyPatch(nameof(CreateObjectMessage.Serialize))]
    [HarmonyPriority(c_Priority)]
    [HarmonyPostfix]
    private static void Serialize(CreateObjectMessage __instance, FastBufferWriter writer)
    {
        if (NetworkManager.Singleton.IsHost)
        {
            return;
        }

        if (!TryGetGrabbableObject(__instance.ObjectInfo, out var grabbableObject))
        {
            return;
        }

        if (!Patch_RoundManager.s_AssignedRandomSpawn.TryGetValue(grabbableObject, out var randomScrapSpawn)
            || randomScrapSpawn == null
            || !randomScrapSpawn.spawnedItemsCopyPosition
            || randomScrapSpawn.spawnWithParent == null)
        {
            goto writeNull;
        }

        var networkObject = randomScrapSpawn.GetComponentInParent<NetworkObject>();
        if (networkObject == null
            || !networkObject.IsSpawned
            || networkObject.transform.parent != null)
        {
            goto writeNull;
        }

        s_Indicies.Clear();
        var current = randomScrapSpawn.transform;

        while (current != null && current != networkObject.transform)
        {
            s_Indicies.Add(current.GetSiblingIndex());
            current = current.parent;
        }

        if (current != networkObject.transform)
        {
            // somehow didn't got to the parent

            goto writeNull;
        }

        s_Indicies.Reverse();

        writer.WriteByteSafe(1);
        writer.WriteNetworkSerializable<NetworkObjectReference>(networkObject);
        writer.WriteValueSafe(s_Indicies.Count);

        foreach (var siblingIndex in s_Indicies)
        {
            writer.WriteValueSafe(siblingIndex);
        }

        return;

    writeNull:
        writer.WriteByteSafe(0);
    }

    [HarmonyPatch(nameof(CreateObjectMessage.Handle))]
    [HarmonyPriority(c_Priority)]
    [HarmonyPostfix]
    public static void Handle(CreateObjectMessage __instance)
    {
        if (!TryGetGrabbableObject(__instance.ObjectInfo, out var grabbableObject))
        {
            return;
        }

        var reader = __instance.m_ReceivedNetworkVariableData;
        if (reader.Length - reader.Position <= 0)
        {
            return;
        }

        reader.ReadByteSafe(out var hasValue);
        if (hasValue == 0)
        {
            Patch_RoundManager.s_AssignedRandomSpawn.AddOrUpdate(grabbableObject, null);
            return;
        }

        if (hasValue != 1)
        {
            // Invalid data (mod conflict?)
            return;
        }

        reader.ReadNetworkSerializable(out NetworkObjectReference networkObjectReference);
        if (!networkObjectReference.TryGet(out NetworkObject networkObject)
            || networkObject == null)
        {
            return;
        }

        reader.ReadValueSafe(out int pathLength);
        if (pathLength < 0)
        {
            return;
        }

        var current = networkObject.transform;
        for (int i = 0; i < pathLength; i++)
        {
            reader.ReadValueSafe(out int siblingIndex);

            if (siblingIndex < 0 || siblingIndex >= current.childCount)
            {
                return;
            }

            current = current.GetChild(siblingIndex);
        }

        var spawn = current.GetComponent<RandomScrapSpawn>();
        if (spawn == null)
        {
            return;
        }

        Patch_RoundManager.s_AssignedRandomSpawn.Add(grabbableObject, spawn);
    }

    private static bool TryGetGrabbableObject(NetworkObject.SceneObject info, [NotNullWhen(true)] out GrabbableObject? grabbableObject)
    {
        var manager = NetworkManager.Singleton;

        if (!manager.SpawnManager.SpawnedObjects.TryGetValue(info.NetworkObjectId, out var no)
            || no.ChildNetworkBehaviours is not { Count: > 0 })
        {
            grabbableObject = null;
            return false;
        }

        grabbableObject = no.ChildNetworkBehaviours.Find(b => b is GrabbableObject) as GrabbableObject;
        return grabbableObject != null;
    }
}
