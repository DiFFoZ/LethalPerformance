using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using DunGen;
using HarmonyLib;
using LethalPerformance.Patcher.API;
using UnityEngine;

namespace LethalPerformance.Dungen;

internal static partial class Patch_Dungeon
{
    private static class Patch_Misc
    {
        [HarmonyCleanup]
        public static Exception? Cleanup(Exception exception)
        {
            return HarmonyExceptionHandler.ReportException(exception);
        }

        [HarmonyPatch(typeof(UnityUtil), nameof(UnityUtil.AreBoundsOverlapping))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> RemoveMinArrayAllocation(IEnumerable<CodeInstruction> instructions)
        {
            var minArray = AccessTools.Method(typeof(Mathf), nameof(Mathf.Min), [typeof(float[])]);
            var min2 = AccessTools.Method(typeof(Mathf), nameof(Mathf.Min), [typeof(float), typeof(float)]);
            var x = AccessTools.Field(typeof(Vector3), nameof(Vector3.x));
            var y = AccessTools.Field(typeof(Vector3), nameof(Vector3.y));
            var z = AccessTools.Field(typeof(Vector3), nameof(Vector3.z));

            // Mathf.Min(new float[] { vector.x, vector.y, vector.z })
            CodeMatch[] matches =
            [
                new(OpCodes.Ldc_I4_3),
                new(OpCodes.Newarr, typeof(float)),
                new(OpCodes.Dup),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Ldloc_0),
                new(OpCodes.Ldfld, x),
                new(OpCodes.Stelem_R4),
                new(OpCodes.Dup),
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Ldloc_0),
                new(OpCodes.Ldfld, y),
                new(OpCodes.Stelem_R4),
                new(OpCodes.Dup),
                new(OpCodes.Ldc_I4_2),
                new(OpCodes.Ldloc_0),
                new(OpCodes.Ldfld, z),
                new(OpCodes.Stelem_R4),
                new(OpCodes.Call, minArray),
            ];

            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, matches)
                .ThrowIfInvalid("Failed to find Mathf.Min(float[]) allocation in UnityUtil.AreBoundsOverlapping")
                .RemoveInstructions(matches.Length)
                .Insert(
                [
                    new(OpCodes.Ldloc_0),
                    new(OpCodes.Ldfld, x),
                    new(OpCodes.Ldloc_0),
                    new(OpCodes.Ldfld, y),
                    new(OpCodes.Call, min2),
                    new(OpCodes.Ldloc_0),
                    new(OpCodes.Ldfld, z),
                    new(OpCodes.Call, min2),
                ]);

            return matcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(DungeonGenerator), nameof(DungeonGenerator.Wait))]
        [HarmonyPrefix]
        public static void FixYieldTimer(DungeonGenerator __instance, ref IEnumerator routine)
        {
            if (!__instance.GenerateAsynchronously)
            {
                return;
            }

            routine = RestartYieldTimerAfterEachYield(__instance, routine);
        }

        private static IEnumerator RestartYieldTimerAfterEachYield(DungeonGenerator generator, IEnumerator routine)
        {
            var timer = generator.yieldTimer;
            timer.Restart();
            try
            {
                while (routine.MoveNext())
                {
                    yield return routine.Current;
                    timer.Restart();
                }
            }
            finally
            {
                if (routine is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
