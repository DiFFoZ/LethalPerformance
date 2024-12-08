using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using ES3Internal;
using HarmonyLib;

namespace LethalPerformance.ES3.Patches;
[HarmonyPatch(typeof(ES3Stream))]
internal static class Patch_ES3Stream
{
    [HarmonyPatch(nameof(ES3Stream.CreateStream), argumentTypes: [typeof(ES3Settings), typeof(ES3FileMode)])]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> RemoveUnusedCode(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        // removes unused constructor call of FileInfo

        matcher.MatchForward(false, [new (ci =>
            ci.opcode == OpCodes.Newobj
            && ci.operand is ConstructorInfo constructor
            && constructor.DeclaringType == typeof(FileInfo))])
            .Advance(-2)
            .RemoveInstructions(4);

        return matcher.InstructionEnumeration();
    }
}
