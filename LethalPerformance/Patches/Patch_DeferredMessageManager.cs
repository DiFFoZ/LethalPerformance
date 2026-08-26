using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using LethalPerformance.Extensions;
using LethalPerformance.Patcher;
using Unity.Netcode;

namespace LethalPerformance.Dev.Patches;
[HarmonyPatch]
internal static class Patch_DeferredMessageManager
{
    [HarmonyPatch(typeof(NetworkBehaviour), nameof(NetworkBehaviour.__endSendServerRpc))]
    [HarmonyPatch(typeof(NetworkBehaviour), nameof(NetworkBehaviour.__endSendClientRpc))]
    [HarmonyPatch(typeof(NetworkBehaviour), nameof(NetworkBehaviour.__endSendRpc))]
    [HarmonyPostfix]
    public static void LogUnspawnedRpcSend(NetworkBehaviour __instance)
    {
        try
        {
            if (__instance.IsSpawned && __instance.NetworkObjectId != 0)
            {
                return;
            }

            LethalPerformancePatcher.Logger.LogFatal(
                $"{__instance.GetType().Assembly.GetName().FullName} sent RPC while not spawned (id:{__instance.NetworkObjectId}, path:{__instance.transform.GetScenePath()})");
            LethalPerformancePatcher.Logger.LogFatal(Environment.StackTrace);
        }
        catch (Exception ex)
        {
            LethalPerformancePatcher.Logger.LogWarning($"Failed to log unspawned RPC send\n{ex}");
        }
    }

    [HarmonyPatch(typeof(DeferredMessageManager), nameof(DeferredMessageManager.PurgeTrigger))]
    [HarmonyPrefix]
    public static void LogPurgedDeferredMessages(DeferredMessageManager.TriggerInfo triggerInfo)
    {
        try
        {
            var logger = LethalPerformancePatcher.Logger;

            for (var i = 0; i < triggerInfo.TriggerData.Length; i++)
            {
                var data = triggerInfo.TriggerData[i];
                var messageType = GetMessageType(data.Header.MessageType);
                var messageTypeName = messageType?.Name ?? $"Unknown({data.Header.MessageType})";

                if (TryReadRpcMetadata(data.Reader, messageType, out var metadata, out var rpcSenderClientId))
                {
                    logger.LogFatal(
                        $"[{i}] {messageTypeName} sender:{data.SenderId} rpcSender:{rpcSenderClientId} " +
                        $"objectId:{metadata.NetworkObjectId} behaviourId:{metadata.NetworkBehaviourId} " +
                        $"rpc:{ResolveRpc(metadata.NetworkRpcMethodId)} hash:{metadata.NetworkRpcMethodId}");
                    continue;
                }

                logger.LogFatal($"[{i}] {messageTypeName} sender:{data.SenderId} size:{data.Header.MessageSize}");
            }
        }
        catch (Exception ex)
        {
            LethalPerformancePatcher.Logger.LogWarning($"Failed to check purged messages\n{ex}");
        }
    }

    private static bool TryReadRpcMetadata(FastBufferReader reader, Type? messageType, out RpcMetadata metadata, out ulong rpcSenderClientId)
    {
        metadata = default;
        rpcSenderClientId = 0;

        if (messageType != typeof(ServerRpcMessage)
            && messageType != typeof(ClientRpcMessage)
            && messageType != typeof(RpcMessage))
        {
            return false;
        }

        if (messageType == typeof(RpcMessage))
        {
            ByteUnpacker.ReadValuePacked(reader, out rpcSenderClientId);
        }

        ByteUnpacker.ReadValueBitPacked(reader, out metadata.NetworkObjectId);
        ByteUnpacker.ReadValueBitPacked(reader, out metadata.NetworkBehaviourId);
        ByteUnpacker.ReadValueBitPacked(reader, out metadata.NetworkRpcMethodId);
        return true;
    }

    private static Type? GetMessageType(uint messageType)
    {
        var types = NetworkManager.Singleton?.MessageManager?.MessageTypes;
        if (types == null || messageType >= types.Length)
        {
            return null;
        }

        return types[messageType];
    }

    private static string ResolveRpc(uint rpcMethodId)
    {
        var lookupTable = new List<(Type behaviourType, string rpc)>();
        foreach (var entry in NetworkBehaviour.__rpc_func_table)
        {
            if (!entry.Value.TryGetValue(rpcMethodId, out var handler))
            {
                continue;
            }

            lookupTable.Add((entry.Key, handler.Method.Name));
        }

        return lookupTable.Count > 0 ? FormatRpcNames(lookupTable) : $"??? ({rpcMethodId})";
    }

    private static string FormatRpcNames(List<(Type behaviourType, string rpc)> names)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < names.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }

            sb.Append(names[i].behaviourType.Name).Append('.').Append(names[i].rpc);
        }

        return sb.ToString();
    }
}
