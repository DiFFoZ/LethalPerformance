using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using LethalPerformance.Extensions;
using LethalPerformance.Patcher;
using Unity.Netcode;
using UnityEngine;

namespace LethalPerformance.Dev.Patches;
// moved from dev, don't forget to reenable it after
[HarmonyPatch(typeof(NetworkObject))]
internal static class Patch_NetworkObject
{
    [HarmonyPatch(nameof(NetworkObject.OnDestroy))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> LogObjectOnFailedDestroy(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        matcher.MatchForward(false, [
            new (OpCodes.Ldstr, "[Invalid Destroy][{0}][NetworkObjectId:{1}] Destroy a spawned {2} on a non-host client is not valid. Call {3} or {4} on the server/host instead.")
            ])
            .Insert([
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => LogStacktrace))
                ]);

        return matcher.Instructions();
    }

    public static void LogStacktrace(NetworkObject @object)
    {
        try
        {
            LethalPerformancePatcher.Logger.LogFatal(@object.gameObject.name);
        }
        catch { }

        try
        {
            LethalPerformancePatcher.Logger.LogFatal(@object.ToString());
        }
        catch { }

        try
        {
            var components = @object.GetComponents<Component>();
            foreach (var c in components)
            {
                LethalPerformancePatcher.Logger.LogFatal(c.GetType().FullDescription());
            }
        }
        catch { }

        try
        {
            LethalPerformancePatcher.Logger.LogFatal(@object.transform.GetScenePath());
        }
        catch { }

        try
        {
            LethalPerformancePatcher.Logger.LogFatal(Environment.StackTrace);
        }
        catch { }
    }
}
