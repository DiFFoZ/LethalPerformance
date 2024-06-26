using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using LethalPerformance.API;
using Object = UnityEngine.Object;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(FoliageDetailDistance))]
internal static class Patch_FoliageDetailDistance
{
    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyPatch(nameof(FoliageDetailDistance.Start))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> ReplaceFindObjectOfType(IEnumerable<CodeInstruction> codeInstructions)
    {
        var matcher = new CodeMatcher(codeInstructions);

        var findObjectOfType = typeof(Object)
            .GetMethod(nameof(Object.FindObjectOfType), 1, AccessTools.all, null, CallingConventions.Any, [], [])
            .MakeGenericMethod(typeof(StartOfRound));

        matcher
            .SearchForward(c => c.Calls(findObjectOfType))
            .ThrowIfInvalid("Failed to find call of FindObjectOfType<StartOfRound>")
            .Operand = typeof(StartOfRound)
                .GetProperty(nameof(StartOfRound.Instance), AccessTools.all)
                .GetGetMethod();

        return matcher.InstructionEnumeration();
    }

    public static void Test(FoliageDetailDistance instance)
    {
        instance.destroyCancellationToken.Register(OnDestroy, instance);

        static void OnDestroy(object instanceObj)
        {
            Console.WriteLine("HEY WE DESTROY");
            if (instanceObj is not FoliageDetailDistance instance)
            {
                return;
            }

            Console.WriteLine("HEY WE DESTROY with instance");
        }
    }
}
