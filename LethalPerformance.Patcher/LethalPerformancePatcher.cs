using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LethalPerformance.Patcher.TomlConverters;
using LethalPerformance.Patcher.Utilities;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

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
    public static IEnumerable<string> TargetDLLs { get; } = ["Assembly-CSharp.dll"];

    // cannot be removed, BepInEx checks it
    // https://github.com/BepInEx/BepInEx/blob/v5-lts/BepInEx.Preloader/Patching/AssemblyPatcher.cs#L67
    public static void Patch(AssemblyDefinition assembly)
    {
        foreach (var type in assembly.MainModule.Types)
        {
            if (type.Name != "AudioReverbPresets")
            {
                continue;
            }

            if (type.GetMethods().Any(md => md.Name == "Awake"))
            {
                Logger.LogInfo("AudioReverbPresets already contains Awake method. Some preloader added it?");
                return;
            }

            var method = new MethodDefinition("Awake", MethodAttributes.Private, assembly.MainModule.TypeSystem.Void);

            var processor = method.Body.GetILProcessor();
            StubMethod(processor);

            type.Methods.Add(method);

            return;
        }
    }

    private static void StubMethod(ILProcessor processor)
    {
        for (var i = 0; i < 32; i++)
        {
            processor.Emit(OpCodes.Nop);
        }
        processor.Emit(OpCodes.Ret);
    }
}
