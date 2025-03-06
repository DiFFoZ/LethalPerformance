using System;
using System.Collections.Generic;
using HarmonyLib;
using LethalPerformance.Patcher.API;
using static System.Reflection.Emit.OpCodes;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(VehicleController))]
internal static class Patch_VehicleController
{
    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyPatch(nameof(VehicleController.FixedUpdate))]
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> NotSearchDropshipAfterDelivery(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        matcher.MatchForward(true, [
            new(Call), // StartOfRound.Instance
            new(Ldfld), // inShipPhase
            new(Brtrue, name: "branch")
            ]);

        var label = matcher.NamedMatch("branch").operand;
        var hasBeenSpawnedField = typeof(VehicleController).GetField(nameof(VehicleController.hasBeenSpawned), AccessTools.all);

        matcher.Advance(1)
            .Insert([
                new(Ldarg_0),
                new(Ldfld, hasBeenSpawnedField),
                new(Brtrue, label)
            ]);

        return matcher.Instructions();
    }
}
