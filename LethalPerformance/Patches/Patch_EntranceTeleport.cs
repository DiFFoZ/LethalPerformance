using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using static System.Reflection.Emit.OpCodes;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(EntranceTeleport))]
internal static class Patch_EntranceTeleport
{
    public static IEnumerable<CodeInstruction> DoNotSearchEntrances(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        // todo: move stfld EntranceTeleport::checkForEnemiesInterval before the check
        // todo: listen to dungeon status change

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
            .Advance(3)
            .RemoveInstructions(6);

        // if (!gotExitPoint) return;

        return matcher.InstructionEnumeration();
    }
}
