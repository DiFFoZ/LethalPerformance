using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LethalPerformance.Patcher.TomlConverters;
using LethalPerformance.Patcher.Utilities;
using Mono.Cecil;

namespace LethalPerformance.Patcher;
public class LethalPerformancePatcher
{
    public static Harmony? Harmony { get; set; }
    public static ManualLogSource Logger { get; } = BepInEx.Logging.Logger.CreateLogSource("LethalPeformance.Patcher");
    public static ConfigSaverTask ConfigSaverTask { get; } = new();

    public static void Finish()
    {
        // Finish() - all assemblies are patched and loaded, should be now safe to access other classes (but still via reflection)

        // let Harmony init other classes, because it's now safe to load them
        Harmony = new Harmony("LethalPerformance.Patcher");
        Harmony.PatchAll(typeof(LethalPerformancePatcher).Assembly);

        // removes compatibility with old harmony
        Harmony.UnpatchID("org.bepinex.fixes.harmonyinterop");

        TomlTypeConverter.TypeConverters[typeof(string)] = new StringTomlConverter();
        TomlTypeConverter.TypeConverters[typeof(bool)] = new BoolTomlConverter();
    }

    // cannot be removed, BepInEx checks it
    public static IEnumerable<string> TargetDLLs { get; } = ["Assembly-CSharp.dll"];

    // cannot be removed, BepInEx checks it
    // https://github.com/BepInEx/BepInEx/blob/v5-lts/BepInEx.Preloader/Patching/AssemblyPatcher.cs#L67
    public static void Patch(AssemblyDefinition assembly)
    {
        Dictionary<string, Action<AssemblyDefinition, TypeDefinition>> workList = new()
        {
            { "AudioReverbPresets", (a, t) => AssemblyPatcherUtilities.AddMethod(a, t, "Awake") },
            { "DepositItemsDesk", (a, t) => AssemblyPatcherUtilities.AddMethod(a, t, "Awake") },
            // todo: detect if BrutalCompanyMinusExtra(Reborn) mod is installed
            // issue: https://github.com/DiFFoZ/LethalPerformance/issues/11
            //{ "animatedSun", (a, t) => AssemblyPatcherUtilities.RemoveMethod(a, t, "Update") },
        };

        var types = assembly.MainModule.Types;
        foreach ((string typeName, Action<AssemblyDefinition, TypeDefinition> action) in workList)
        {
            var type = types.FirstOrDefault(t => t.Name == typeName);
            if (type == null)
            {
                Logger.LogWarning("Failed to patch " + typeName);
                continue;
            }

            action(assembly, type);
        }
    }
}
