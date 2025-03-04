using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Bootstrap;
using HarmonyLib;
using LethalPerformance.Patcher.API;
using UnityEngine;

namespace LethalPerformance.Patches.Mods;
[HarmonyPatch]
internal static class Patch_MoreSuits
{
    private static readonly MethodInfo? s_MethodToPatch;

    static Patch_MoreSuits()
    {
        if (!Chainloader.PluginInfos.TryGetValue(Dependencies.MoreSuits, out var pluginInfo))
        {
            return;
        }

        var patchClass = AccessTools.Inner(pluginInfo.Instance.GetType(), "StartOfRoundPatch");
        if (patchClass != null)
        {
            s_MethodToPatch = patchClass.GetMethod("StartPatch", AccessTools.all);
        }

        if (s_MethodToPatch == null)
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning("Failed to find MoreSuits method to patch");
        }
    }

    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyPrepare]
    public static bool ShouldPatch()
    {
        return s_MethodToPatch != null;
    }

    [HarmonyTargetMethod]
    public static MethodBase GetTargetMethod()
    {
        return s_MethodToPatch!;
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> OptimizeSuitsTextures(IEnumerable<CodeInstruction> codeInstructions)
    {
        // new Texture2D(2, 2, TextureFormat.DXT5, true)

        var matcher = new CodeMatcher(codeInstructions);

        var textureConstructor = typeof(Texture2D)
            .GetConstructor([typeof(int), typeof(int), typeof(TextureFormat), typeof(bool)]);

        matcher.MatchForward(false,
            [
            new(OpCodes.Ldc_I4_2),
            new(OpCodes.Ldc_I4_2),
            new(OpCodes.Newobj)
            ])
            .Repeat(m =>
            {
                m.SetOpcodeAndAdvance(OpCodes.Ldc_I4_4)
                .SetOpcodeAndAdvance(OpCodes.Ldc_I4_4)
                .InsertAndAdvance([new(OpCodes.Ldc_I4, (int)TextureFormat.DXT5), new(OpCodes.Ldc_I4_1)])
                .SetOperandAndAdvance(textureConstructor);
            });

        return matcher.InstructionEnumeration();
    }
}
