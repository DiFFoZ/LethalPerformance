using System.Collections.Generic;
using System.Reflection;
using DunGen;
using HarmonyLib;
using static System.Reflection.Emit.OpCodes;

namespace LethalPerformance.Patches;
[HarmonyPatch]
internal static class Patch_RoundManager
{
    [HarmonyTargetMethod]
    public static MethodBase GetTargetMethod()
    {
        var enumerator = typeof(RoundManager).GetMethod(nameof(RoundManager.waitForMainEntranceTeleportToSpawn), AccessTools.all);
        return AccessTools.EnumeratorMoveNext(enumerator);
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> WaitForDungeonToComplete(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        var findMainEntrancePositionMethod = typeof(RoundManager)
            .GetMethod(nameof(RoundManager.FindMainEntrancePosition), AccessTools.all);

        matcher
            .MatchForward(false, [
            new(Call, findMainEntrancePositionMethod)
            ])
            .ThrowIfInvalid("Failed to find call of finding main entrance")
            .Advance(-2)
            .Set(Call, SymbolExtensions.GetMethodInfo(() => IsDungeonLoaded))
            .Advance(1)
            .RemoveInstructions(4)
            .SetOpcodeAndAdvance(Brtrue);

        return matcher.Instructions();
    }

    private static bool IsDungeonLoaded()
    {
        var dungeon = Object.FindAnyObjectByType<RuntimeDungeon>();
        if (dungeon == null)
        {
            return false;
        }

        return dungeon.Generator.Status is GenerationStatus.Failed or GenerationStatus.Complete;
    }
}
