using System.Collections.Generic;
using System.Reflection;
using DunGen;
using HarmonyLib;
using static System.Reflection.Emit.OpCodes;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(RoundManager))]
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
            .Start()
            .MatchForward(false, [
            new(Call, findMainEntrancePositionMethod)
            ])
            .ThrowIfInvalid("Failed to find call of finding main entrance")
            .Advance(-2)
            .Insert([
                new(Call, SymbolExtensions.GetMethodInfo(() => IsDungeonLoaded))
                ])
            .AddLabelsAt(matcher.Pos - 1, matcher.Labels)
            .RemoveInstructions(5)
            .Opcode = Brtrue;


        return instructions;
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
