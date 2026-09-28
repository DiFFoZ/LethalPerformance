using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LethalPerformance.Patcher.API;
using Unity.Netcode;
using UnityEngine;

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
            Patch_RoundManager.s_AssignedRandomSpawn.Remove(item);

            if (spawn != null)
            {
                LethalPerformancePlugin.Instance.Logger.LogDebug($"{item?.itemProperties?.itemName ?? "NULL"} used random scrap spawn");
                return [spawn];
            }

            // Got null caching, so client acknowledged that item doesn't have RandomScrapSpawn attached to (see Patch_CreateObjectMessage).
            // Or it just got destroyed between Spawn and Start?
            return [];
        }

        if (!NetworkManager.Singleton.IsHost)
        {
            // Host didn't sent any data (or it was invalid) if item did have RandomScrapSpawn.
            // so fallback to searching the scene

            return Object.FindObjectsByType<RandomScrapSpawn>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }

        // Spawned outside of RoundManager.SpawnScrapInLevel (see Patch_RoundManager)
        return [];
    }
}
