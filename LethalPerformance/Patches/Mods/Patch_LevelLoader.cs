using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Bootstrap;
using DunGen;
using HarmonyLib;
using LethalPerformance.Patcher.API;
using LethalPerformance.Patches.ReferenceHolder;

namespace LethalPerformance.Patches.Mods;
[HarmonyPatch]
internal static class Patch_LevelLoader
{
    private static readonly MethodInfo? s_MethodToPatch;

    static Patch_LevelLoader()
    {
        if (!Chainloader.PluginInfos.TryGetValue(Dependencies.LethalLevelLoader, out var pluginInfo))
        {
            return;
        }

        var lllAssembly = pluginInfo.Instance.GetType().Assembly;

        var levelLoaderType = lllAssembly.GetType("LethalLevelLoader.LevelLoader", false);
        if (levelLoaderType != null)
        {
            s_MethodToPatch = levelLoaderType.GetMethod("RestoreRuntimeDungeon", AccessTools.all);
        }

        if (s_MethodToPatch == null)
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning("Failed to find LethalLevelLoader method to patch");
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
    public static IEnumerable<CodeInstruction> GetReferenceToDungeon(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);
        matcher.MatchForward(true,
            [
            new(OpCodes.Ldloc_0),
            new(OpCodes.Callvirt),
            new(OpCodes.Stloc_1),
            ])
            .ThrowIfInvalid("Failed to find where dungeon add component is called")
            .Advance(1)
            .Insert([
                new(OpCodes.Ldloc_1),
                CodeInstruction.Call(() => LLLDungeonStore)
                ]);

        return matcher.InstructionEnumeration();
    }

    public static void LLLDungeonStore(RuntimeDungeon dungeon)
    {
        LethalPerformancePlugin.Instance.Logger.LogInfo("Got dungeon from LLL (fallback path)");

        MoonCachingPatch.s_RuntimeDungeon.SetInstance(dungeon);
    }
}
