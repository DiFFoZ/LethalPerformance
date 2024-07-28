using System.Collections.Generic;
using HarmonyLib;
using Mono.Cecil;

namespace LethalPerformance.Patcher;

internal class LethalPerformancePatcher
{
    private static Harmony? s_Harmony;

    public static void Finish()
    {
        // Finish() - all assemblies are patched and loaded, should be now safe to access other classes (but still via reflection)

        // let Harmony init other classes, because it's now safe to load them
        s_Harmony = new Harmony("LethalPerformance.Patcher");
        s_Harmony.PatchAll(typeof(LethalPerformancePatcher).Assembly);
    }

    // cannot be removed, BepInEx checks it
    public static IEnumerable<string> TargetDLLs { get; } = ["Assembly-CSharp"];

    // cannot be removed, BepInEx checks it
    // https://github.com/BepInEx/BepInEx/blob/v5-lts/BepInEx.Preloader/Patching/AssemblyPatcher.cs#L67
    public static void Patch(AssemblyDefinition _)
    {
    }
}
