using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LethalPerformance.Patcher.API;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(GrabbableObject))]
internal static class Patch_GrabbableObject
{
    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyPatch(nameof(GrabbableObject.Start))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> UseCachedRandomSpawnScrap(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        var findObjectOfTypeMethod = typeof(Object)
          .GetMethod(nameof(Object.FindObjectsOfType), 1, AccessTools.all, null, CallingConventions.Any, [], [])
          .MakeGenericMethod(typeof(RandomScrapSpawn));

        matcher.SearchForward(c => c.Calls(findObjectOfTypeMethod))
            .Operand = SymbolExtensions.GetMethodInfo(() => GetAssignedRandomScrapSpawn);

        matcher.Insert([
            new (OpCodes.Ldarg_0)
            ]);

        return matcher.InstructionEnumeration();
    }

    public static RandomScrapSpawn[] GetAssignedRandomScrapSpawn(GrabbableObject item)
    {
        if (Patch_RoundManager.s_AssignedRandomSpawn.TryGetValue(item, out var spawn))
        {
            LethalPerformancePlugin.Instance.Logger.LogDebug("Used RandomScrapSpawn");

            Patch_RoundManager.s_AssignedRandomSpawn.Remove(item);

            return [spawn];
        }

        return [];
    }
}
