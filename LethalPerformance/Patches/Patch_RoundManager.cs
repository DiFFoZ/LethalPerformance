using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using LethalPerformance.Patcher.API;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(RoundManager))]
internal static class Patch_RoundManager
{
    internal static readonly ConditionalWeakTable<GrabbableObject, RandomScrapSpawn> s_AssignedRandomSpawn = new();

    private static RandomScrapSpawn? s_RandomScrapSpawn;

    [HarmonyCleanup]
    private static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyPatch(nameof(RoundManager.SpawnScrapInLevel))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CatchRandomScrapSpawn(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        var randomScrapSpawnLocal = matcher.SearchForward(ci => ci.operand is LocalBuilder lb && lb.LocalType == typeof(RandomScrapSpawn))
            .ThrowIfInvalid("Failed to find localbuilder of RandomScrapSpawn")
            .Operand as LocalBuilder;

        var spawnWithParentField = typeof(RandomScrapSpawn).GetField(nameof(RandomScrapSpawn.spawnWithParent), AccessTools.all);

        matcher.MatchForward(false,
            [
            new(OpCodes.Ldloc_S, randomScrapSpawnLocal),
            new(OpCodes.Ldfld, spawnWithParentField),
            new(OpCodes.Callvirt),
            new(OpCodes.Callvirt),
            new(OpCodes.Stloc_S),
            new(OpCodes.Br),
            ])
            .ThrowIfInvalid("Failed to find patch point")
            .Insert(
            [
                new(OpCodes.Ldloc, randomScrapSpawnLocal),
                CodeInstruction.Call(() => CaptureScrapSpawn)
            ]);

        var grabbableObjectLocal = matcher.SearchForward(ci => ci.operand is LocalBuilder lb && lb.LocalType == typeof(GrabbableObject))
           .ThrowIfInvalid("Failed to find localbuilder of GrabbableObject")
           .Operand as LocalBuilder;

        matcher.MatchForward(false,
            [
            new(OpCodes.Ldloc_S, grabbableObjectLocal),
            ])
            .Insert(
            [
                new(OpCodes.Ldloc, grabbableObjectLocal),
                CodeInstruction.Call(() => AssignScrapSpawnToObject)
            ]);

        return matcher.InstructionEnumeration();
    }

    private static void CaptureScrapSpawn(RandomScrapSpawn randomScrapSpawn)
    {
        s_RandomScrapSpawn = randomScrapSpawn;
    }

    private static void AssignScrapSpawnToObject(GrabbableObject grabbableObject)
    {
        if (s_RandomScrapSpawn == null)
        {
            return;
        }

        s_AssignedRandomSpawn.AddOrUpdate(grabbableObject, s_RandomScrapSpawn!);
        s_RandomScrapSpawn = null;
    }
}
