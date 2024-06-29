using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LethalPerformance.API;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(ItemDropship))]
internal static class Patch_ItemDropship
{
    private static readonly FieldInfo s_RopesField = typeof(ItemDropship).GetField("ropes", AccessTools.all);

    [HarmonyPrepare]
    public static bool ShouldPatch()
    {
        return s_RopesField != null;
    }

    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(ItemDropship.Update))]
    public static IEnumerable<CodeInstruction> FixAccessingNullArray(IEnumerable<CodeInstruction> codeInstructions)
    {
        var matcher = new CodeMatcher(codeInstructions);

        // find label to jump
        var loadFieldPos = matcher.SearchForward(c => c.LoadsField(s_RopesField)).Pos;
        var label = (Label)matcher.SearchForward(c => c.operand is Label).Operand;

        // move to ldarg.0
        matcher.Start().Advance(loadFieldPos - 1);

        matcher.Insert(new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, s_RopesField),
            new CodeInstruction(OpCodes.Brfalse_S, label));

        return matcher.InstructionEnumeration();
    }
}
