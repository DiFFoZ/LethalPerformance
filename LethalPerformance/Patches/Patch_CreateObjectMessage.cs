using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.Netcode;

namespace LethalPerformance.Patches;

[HarmonyPatch(typeof(CreateObjectMessage))]
internal static class Patch_CreateObjectMessage
{
#if ENABLE_PROFILER
    private const bool c_DontSerialize = false;
    private const bool c_DontRead = false;
#endif

    private const byte c_Ok = 0;
    private const byte c_OkNoData = 1;
    private const byte c_Error = 2;

    // A little compact with in future mod -- idk
    private const int c_Priority = Priority.Normal - 1;

    private static readonly List<ushort> s_Indicies = new();

    [HarmonyPatch(nameof(CreateObjectMessage.Serialize))]
    [HarmonyPriority(c_Priority)]
    [HarmonyPostfix]
    private static void Serialize(CreateObjectMessage __instance, FastBufferWriter writer)
    {
#if ENABLE_PROFILER
        if (c_DontSerialize)
        {
            return;
        }
#endif

        if (!TryGetGrabbableObject(__instance.ObjectInfo, out var grabbableObject))
        {
            return;
        }

        if (!Patch_RoundManager.s_AssignedRandomSpawn.TryGetValue(grabbableObject, out var randomScrapSpawn)
            || randomScrapSpawn == null
            || !randomScrapSpawn.spawnedItemsCopyPosition
            || randomScrapSpawn.spawnWithParent == null)
        {
            goto writeNoData;
        }

        var networkObject = randomScrapSpawn.GetComponentInParent<NetworkObject>();
        if (networkObject == null
            || !networkObject.IsSpawned
            || networkObject.transform.parent != null)
        {
            goto writeError;
        }

        s_Indicies.Clear();
        var current = randomScrapSpawn.transform;

        while (current != null && current != networkObject.transform)
        {
            var siblingIndex = current.GetSiblingIndex();

            if (siblingIndex > ushort.MaxValue)
            {
                goto writeError;
            }

            s_Indicies.Add((ushort)siblingIndex);
            current = current.parent;
        }

        if (current != networkObject.transform
            || s_Indicies.Count > ushort.MaxValue)
        {
            goto writeError;
        }

        s_Indicies.Reverse();

        writer.WriteByteSafe(c_Ok);
        writer.WriteNetworkSerializable<NetworkObjectReference>(networkObject);
        writer.WriteValueSafe((ushort)s_Indicies.Count);

        foreach (var siblingIndex in s_Indicies)
        {
            writer.WriteValueSafe(siblingIndex);
        }

        return;

    writeNoData:
        writer.WriteByteSafe(c_OkNoData);
        return;

    writeError:
        writer.WriteByteSafe(c_Error);
    }

    [HarmonyPatch(nameof(CreateObjectMessage.Deserialize))]
    [HarmonyPriority(c_Priority)]
    [HarmonyPostfix]
    public static void Handle(CreateObjectMessage __instance, bool __result)
    {
#if ENABLE_PROFILER
        if (c_DontRead)
        {
            return;
        }
#endif

        if (!__result)
        {
            // Deferred (scene object, will just ignore)
            return;
        }

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
        if (hasValue == c_OkNoData)
        {
            Patch_RoundManager.s_AssignedRandomSpawn.AddOrUpdate(grabbableObject, null);
            return;
        }

        if (hasValue != c_Ok)
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

        reader.ReadValueSafe(out ushort pathLength);

        var current = networkObject.transform;
        for (int i = 0; i < pathLength; i++)
        {
            reader.ReadValueSafe(out ushort siblingIndex);

            if (siblingIndex >= current.childCount)
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
