using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using TMPro;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(HangarShipDoor))]
internal static class Patch_HangarShipDoor
{
    [HarmonyPatch(nameof(HangarShipDoor.Update))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> FixFormatAllocation(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        // todo: add check if text should be updated?

        matcher.MatchForward(false,
            new(OpCodes.Box),
            new(OpCodes.Call),
            new(OpCodes.Callvirt))
            .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Conv_R4)) // convert int to float
            .RemoveInstruction() // remove string.Format call
            .Set(OpCodes.Call, typeof(TMP_Text).GetMethod(nameof(TMP_Text.SetText), [typeof(string), typeof(float)]));

        return matcher.InstructionEnumeration();
    }
}
