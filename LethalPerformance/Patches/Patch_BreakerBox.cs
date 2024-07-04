using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using LethalPerformance.API;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(BreakerBox))]
internal static class Patch_BreakerBox
{
    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyTargetMethods]
    public static IEnumerable<MethodInfo> GetPatchingMethods()
    {
        var breakerBoxType = typeof(BreakerBox);

        yield return breakerBoxType.GetMethod(nameof(BreakerBox.Start), AccessTools.all);
        yield return breakerBoxType.GetMethod(nameof(BreakerBox.SetSwitchesOff), AccessTools.all);
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> ReplaceFindObjectOfType(IEnumerable<CodeInstruction> codeInstructions)
    {
        var matcher = new CodeMatcher(codeInstructions);

        var findObjectOfType = typeof(Object)
            .GetMethod(nameof(Object.FindObjectOfType), 1, AccessTools.all, null, CallingConventions.Any, [], [])
            .MakeGenericMethod(typeof(RoundManager));

        matcher.SearchForward(c => c.Calls(findObjectOfType));

        if (matcher.IsValid)
        {
            matcher
                .Operand = typeof(RoundManager)
                .GetProperty(nameof(RoundManager.Instance), AccessTools.all)
                .GetGetMethod();
        }

        return matcher.InstructionEnumeration();
    }
}
