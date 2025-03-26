using System;
using System.Collections.Generic;
using HarmonyLib;
using LethalPerformance.Patcher.API;
using static System.Reflection.Emit.OpCodes;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(EntranceTeleport))]
internal static class Patch_EntranceTeleport
{
    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyPatch(nameof(EntranceTeleport.Update))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> DoNotSearchEntrances(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        // todo: listen to dungeon status change

        var checkForEnemiesIntervalField = typeof(EntranceTeleport)
            .GetField(nameof(EntranceTeleport.checkForEnemiesInterval), AccessTools.all);

        matcher.MatchForward(false, [
            new(Ldarg_0),
            new(Ldfld), // EntranceTeleport::gotExitPoint
            new(Brtrue),
            new(Ldarg_0),
            new(Call), // EntranceTeleport::FindExitPoint()
            new(Brfalse),
            new(Ldarg_0),
            new(Ldc_I4_1),
            new(Stfld), // EntranceTeleport::gotExitPoint
            // ret (br to the end)
            ])
            // store 1f to the EntranceTeleport::checkForEnemiesInterval
            .Insert([
                new(Ldarg_0),
                new(Ldc_R4, 1f),
                new(Stfld, checkForEnemiesIntervalField)
                ])
            .Advance(3)
            .RemoveInstructions(6);

        // if (!gotExitPoint) return;

        return matcher.InstructionEnumeration();
    }
}
