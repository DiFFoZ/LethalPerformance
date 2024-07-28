using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;

namespace LethalPerformance.Patcher.Patches;
[HarmonyPatch(typeof(ConfigDefinition))]
internal static class Patch_ConfigDefinition
{
    private static readonly MethodInfo s_TargetMethod = typeof(Patch_ConfigDefinition)
        .GetMethod(nameof(CheckInvalidConfigCharsOptimized), AccessTools.all);

    [HarmonyPatch(nameof(ConfigDefinition.CheckInvalidConfigChars))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> TargetOtherMethod(IEnumerable<CodeInstruction> _)
    {
        return
            [
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldarg_1),
            new(OpCodes.Call, s_TargetMethod),
            new(OpCodes.Ret)
            ];
    }

    public static void CheckInvalidConfigCharsOptimized(string val, string name)
    {
        if (val == null)
        {
            throw new ArgumentNullException(name);
        }

        if (!val.AsSpan().Trim().SequenceEqual(val))
        {
            throw new ArgumentException("Cannot use whitespace characters at start or end of section and key names", name);
        }

        if (val.AsSpan().IndexOfAny(ConfigDefinition._invalidConfigChars) >= 0)
        {
            throw new ArgumentException("Cannot use any of the following characters in section and key names: = \\n \\t \\ \" ' [ ]", name);
        }
    }
}
