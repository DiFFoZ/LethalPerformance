using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using LethalPerformance.API;
using LethalPerformance.Patcher.API;
using UnityEngine;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(AudioReverbTrigger), nameof(AudioReverbTrigger.OnTriggerStay))]
internal static class Patch_AudioReverbTrigger
{
    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> FixTagAllocation(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        var gameObjectTagGetter = typeof(GameObject).GetProperty(nameof(GameObject.tag), AccessTools.all).GetMethod;

        matcher.MatchForward(false,
            new(OpCodes.Callvirt, gameObjectTagGetter),
            new(OpCodes.Ldstr),
            new(OpCodes.Callvirt))
            .RemoveInstructions(3)
            .Insert(CodeInstruction.Call((GameObject x) => ObjectExtensions.ComparePlayerRagdollTag(x)));

        return matcher.InstructionEnumeration();
    }
}
