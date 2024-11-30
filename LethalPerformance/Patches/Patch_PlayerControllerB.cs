using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using LethalPerformance.Patcher.API;
using UnityEngine;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(PlayerControllerB))]
internal static class Patch_PlayerControllerB
{
    private static Vector3[] s_Normals = null!;

    // todo: SetHoverTipAndCurrentInteractTrigger fix tag

    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    /// <summary>
    /// Fixes local username billboard actives and deactives in the same frame
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(PlayerControllerB.ShowNameBillboard))]
    public static bool FixLocalBillBoardIsEnabling(PlayerControllerB __instance, bool __runOriginal)
    {
        if (!__runOriginal)
        {
            return __runOriginal;
        }

        var isLocalPlayer = __instance == StartOfRound.Instance.localPlayerController;
        return !isLocalPlayer;
    }

    [HarmonyPatch(nameof(PlayerControllerB.CalculateGroundNormal))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> UseStaticField(IEnumerable<CodeInstruction> instructions)
    {
        var list = instructions.ToList();

        var ldc0 = OpCodes.Ldc_I4_0.Value;

        var opcode = list[0].opcode;
        var diff = opcode.Value - ldc0;

        // failed to find length of array
        if (diff < 0 || diff >= OpCodes.Ldc_I4_8.Value)
        {
            return list;
        }

        s_Normals = new Vector3[diff + 1];

        // ldc.i4.5
        // newarr
        list.RemoveRange(0, 2);

        var normalsField = typeof(Patch_PlayerControllerB).GetField(nameof(s_Normals), AccessTools.all);
        list.Insert(0, new(OpCodes.Ldsfld, normalsField));

        return list;
    }
}
