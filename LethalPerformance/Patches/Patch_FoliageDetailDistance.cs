using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using LethalPerformance.Patcher.API;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(FoliageDetailDistance))]
internal static class Patch_FoliageDetailDistance
{
    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyPatch(nameof(FoliageDetailDistance.Update))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> RemoveMaterialCheck(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        var allBushRenderersField = typeof(FoliageDetailDistance)
            .GetField(nameof(FoliageDetailDistance.allBushRenderers), AccessTools.all);

        CodeMatch[] matches = [
            new(OpCodes.Ldarg_0, name: "start"),
            new(OpCodes.Ldfld, allBushRenderersField), // allBushRenderers
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldfld), // bushIndex
            new(OpCodes.Callvirt), // get_item
            new(OpCodes.Callvirt), // get_material
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldfld),
            new(OpCodes.Call),
            new(OpCodes.Brfalse), // op_Inequality
            ];

        matcher.MatchForward(false, matches)
            .Repeat(match =>
        {
            match.RemoveInstructions(matches.Length);
            match.NamedMatch("start").MoveLabelsTo(match.Instruction);
        });

        return matcher.InstructionEnumeration();
    }
}
