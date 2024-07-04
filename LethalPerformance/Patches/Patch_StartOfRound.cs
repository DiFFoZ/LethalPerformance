using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LethalPerformance.API;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(StartOfRound))]
internal static class Patch_StartOfRound
{
    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyPatch(nameof(StartOfRound.SetPlayerSafeInShip))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> ReplaceFindOfObjectOfType(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions);

        // locals
        var countLocal = generator.DeclareLocal(typeof(int), false);

        var findObjectOfTypeMethod = typeof(Object)
           .GetMethod(nameof(Object.FindObjectsOfType), 1, AccessTools.all, null, CallingConventions.Any, [], [])
           .MakeGenericMethod(typeof(EnemyAI));

        var roundManagerInstanceGetter = typeof(RoundManager)
                .GetProperty(nameof(RoundManager.Instance), AccessTools.all)
                .GetGetMethod();

        var spawnedEnemiesField = typeof(RoundManager).GetField(nameof(RoundManager.SpawnedEnemies), AccessTools.all);

        var listInternalArrayField = typeof(List<EnemyAI>)
            .GetField("_items", AccessTools.all);

        var listGetCountMethod = typeof(List<EnemyAI>)
            .GetProperty(nameof(List<EnemyAI>.Count), AccessTools.all)
            .GetGetMethod();

        matcher.SearchForward(c => c.Calls(findObjectOfTypeMethod))
            .Set(OpCodes.Call, roundManagerInstanceGetter)
            .Advance(1)
            .InsertAndAdvance(new(OpCodes.Ldfld, spawnedEnemiesField),
            new(OpCodes.Ldfld, listInternalArrayField)) // unsafe, but allows to other mods to transpiler this method
            .Advance(1) // move after stfld array
            .InsertAndAdvance(new(OpCodes.Call, roundManagerInstanceGetter),
            new(OpCodes.Ldfld, spawnedEnemiesField), // store count to local var
            new(OpCodes.Callvirt, listGetCountMethod),
            new(OpCodes.Stloc, countLocal));

        // replace array.Length to local var Count
        matcher.MatchForward(false, new(OpCodes.Ldloc_1), new(OpCodes.Ldlen))
            .Repeat(m =>
            {
                m.RemoveInstructions(2)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc, countLocal));
            });

        return matcher.InstructionEnumeration();
    }
}
