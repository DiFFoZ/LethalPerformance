using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LethalPerformance.Patcher.TomlConverters;
using LethalPerformance.Patcher.Utilities;
using Mono.Cecil;

namespace LethalPerformance.Patcher;
public class LethalPerformancePatcher
{
    private static Harmony? s_Harmony;
    public static ManualLogSource Logger { get; } = BepInEx.Logging.Logger.CreateLogSource("LethalPeformance.Patcher");
    public static ConfigSaverTask ConfigSaverTask { get; } = new();

    public static void Finish()
    {
        // Finish() - all assemblies are patched and loaded, should be now safe to access other classes (but still via reflection)

        // let Harmony init other classes, because it's now safe to load them
        s_Harmony = new Harmony("LethalPerformance.Patcher");
        s_Harmony.PatchAll(typeof(LethalPerformancePatcher).Assembly);

        // removes compatibility with old harmony
        Harmony.UnpatchID("org.bepinex.fixes.harmonyinterop");

        TomlTypeConverter.TypeConverters[typeof(string)] = new StringTomlConverter();
        TomlTypeConverter.TypeConverters[typeof(bool)] = new BoolTomlConverter();
    }

    // cannot be removed, BepInEx checks it
    public static IEnumerable<string> TargetDLLs { get; } = [];

    // cannot be removed, BepInEx checks it
    // https://github.com/BepInEx/BepInEx/blob/v5-lts/BepInEx.Preloader/Patching/AssemblyPatcher.cs#L67
    public static void Patch(AssemblyDefinition _)
    {
    }
}
