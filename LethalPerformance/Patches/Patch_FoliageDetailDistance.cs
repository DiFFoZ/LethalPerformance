using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using LethalPerformance.API;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(FoliageDetailDistance))]
internal static class Patch_FoliageDetailDistance
{
    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyPatch(nameof(FoliageDetailDistance.Start))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> ReplaceFindObjectOfType(IEnumerable<CodeInstruction> codeInstructions)
    {
        var matcher = new CodeMatcher(codeInstructions);

        var findObjectOfType = typeof(Object)
            .GetMethod(nameof(Object.FindObjectOfType), 1, AccessTools.all, null, CallingConventions.Any, [], [])
            .MakeGenericMethod(typeof(StartOfRound));

        matcher.SearchForward(c => c.Calls(findObjectOfType));

        if (matcher.IsValid)
        {
            matcher
                .Operand = typeof(StartOfRound)
                .GetProperty(nameof(StartOfRound.Instance), AccessTools.all)
                .GetGetMethod();
        }

        return matcher.InstructionEnumeration();
    }
}
