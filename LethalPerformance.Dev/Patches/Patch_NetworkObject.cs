using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using LethalPerformance.Patcher;
using Unity.Netcode;
using UnityEngine;

namespace LethalPerformance.Dev.Patches;
[HarmonyPatch(typeof(NetworkObject))]
internal static class Patch_NetworkObject
{
    [HarmonyPatch("OnDestroy")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> LogObjectOnFailedDestroy(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        matcher.MatchForward(false, [
            new (OpCodes.Ldstr, "Destroy a spawned NetworkObject on a non-host client is not valid. Call Destroy or Despawn on the server/host instead.")
            ])
            .Insert([
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => LogStacktrace))
                ]);

        return matcher.Instructions();
    }

    public static string GetScenePath(this Transform transform)
    {
        var sb = new StringBuilder();
        sb.Append('/').Append(transform.name);

        Transform parent;
        while (parent = transform.parent)
        {
            sb.Insert(0, parent.name)
                .Insert(0, '/');

            transform = parent;
        }

        return sb.ToString();
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
            LethalPerformancePatcher.Logger.LogFatal(GetScenePath(@object.transform));
        }
        catch { }

        try
        {
            LethalPerformancePatcher.Logger.LogFatal(Environment.StackTrace);
        }
        catch { }
    }
}
