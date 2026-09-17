using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LethalPerformance.Patcher.AssemblyPatches;
using LethalPerformance.Patcher.TomlConverters;
using LethalPerformance.Patcher.Utilities;
using Mono.Cecil;

namespace LethalPerformance.Patcher;

public class LethalPerformancePatcher
{
    internal static Harmony? Harmony { get; set; }
    internal static ManualLogSource Logger { get; } = BepInEx.Logging.Logger.CreateLogSource("LethalPerformance.Patcher");
    public static ConfigSaverTask ConfigSaverTask { get; } = new();

    public static event Action? OnModsLoaded;

    internal static void InvokeOnModsLoaded()
    {
        try
        {
            OnModsLoaded?.Invoke();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }

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

        WarnIfTerbiumInstalled();
    }

    private static void WarnIfTerbiumInstalled()
    {
        // BepInEx.Patcher doesn't exists on nuget feed :(

        var patcherPluginsList = (IList)Type.GetType("BepInEx.Preloader.Patching.AssemblyPatcher,BepInEx.Preloader")
            .GetProperty("PatcherPlugins", AccessTools.all)
            .GetGetMethod()
            .Invoke(null, null);

        var typeNameProperty = patcherPluginsList[0]
            .GetType()
            .GetProperty("TypeName", AccessTools.all);

        foreach (var patcher in patcherPluginsList)
        {
            var typeName = (string)typeNameProperty.GetGetMethod().Invoke(patcher, null);
            if (typeName == "Terbium.TerbiumPreloader")
            {
                Logger.LogWarning("Terbium mod installed, for better compatibility remove it.");
                break;
            }
        }
    }

    // cannot be removed, BepInEx checks it
    public static IEnumerable<string> TargetDLLs { get; } = ["Assembly-CSharp.dll", "Assembly-CSharp-firstpass.dll", "DunGen.dll"];

    // cannot be removed, BepInEx checks it
    // https://github.com/BepInEx/BepInEx/blob/v5-lts/BepInEx.Preloader/Patching/AssemblyPatcher.cs#L67
    public static void Patch(AssemblyDefinition assembly)
    {
        switch (assembly.Name.Name)
        {
            case "Assembly-CSharp-firstpass":
                Patch_ES3.Patch(assembly);
                break;
            case "DunGen":
                Patch_ProxyDoorwayConnection.Patch(assembly);
                break;
            case "Assembly-CSharp":
                Patch_AssemblyCSharp.Patch(assembly);
                break;
        }
    }
}