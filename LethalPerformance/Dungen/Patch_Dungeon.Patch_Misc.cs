using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DunGen;
using DunGen.Graph;
using HarmonyLib;
using LethalPerformance.Patcher.API;
using UnityEngine;

namespace LethalPerformance.Dungen;

internal static partial class Patch_Dungeon
{
    private static class Patch_Misc
    {
        private static GraphNode[]? s_SortedNodes;
        private static DungeonFlow? s_CachedFlow;

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

        [HarmonyPatch(typeof(DungeonGenerator), nameof(DungeonGenerator.GenerateMainPath), MethodType.Enumerator)]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> CacheSortedNodes(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            var nodesField = AccessTools.Field(typeof(DungeonFlow), nameof(DungeonFlow.Nodes));

            matcher.MatchForward(false, [
                new(OpCodes.Ldfld, nodesField),
            new(i => i.opcode == OpCodes.Ldsfld && i.operand is FieldInfo fi && fi.FieldType == typeof(Func<GraphNode, float>)),
            ])
                .ThrowIfInvalid("Failed to find DungeonFlow.Nodes");

            var start = matcher.Pos;

            matcher.SearchForward((i) =>
            {
                if (i.opcode != OpCodes.Call || i.operand is not MethodInfo method || method.Name != nameof(Enumerable.ToArray))
                {
                    return false;
                }

                var arguments = method.GetGenericArguments();
                return arguments.Length == 1 && arguments[0] == typeof(GraphNode);
            })
                .ThrowIfInvalid("Failed to find Nodes.ToArray()");

            var end = matcher.Pos;

            matcher.Start()
                .Advance(start)
                .RemoveInstructions(end - start + 1)
                .Insert([
                    new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => GetSortedNodes))
                    ]);

            return matcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(DungeonGenerator), nameof(DungeonGenerator.Generate))]
        [HarmonyPrefix]
        public static void Generate()
        {
            s_SortedNodes = null;
            s_CachedFlow = null;
        }

        private static GraphNode[] GetSortedNodes(DungeonFlow flow)
        {
            if (s_SortedNodes != null && s_CachedFlow == flow)
            {
                return s_SortedNodes;
            }

            s_CachedFlow = flow;
            s_SortedNodes = flow.Nodes.OrderBy(static x => x.Position).ToArray();
            return s_SortedNodes;
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
