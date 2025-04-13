using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LethalPerformance.Patcher.TomlConverters;
using LethalPerformance.Patcher.Utilities;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;

namespace LethalPerformance.Patcher;
public class LethalPerformancePatcher
{
    internal static Harmony? Harmony { get; set; }
    internal static ManualLogSource Logger { get; } = BepInEx.Logging.Logger.CreateLogSource("LethalPerformance.Patcher");
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

        MonoJitConfig.Initialize();
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
    public static IEnumerable<string> TargetDLLs { get; } = ["Assembly-CSharp.dll", "Assembly-CSharp-firstpass.dll"];

    // cannot be removed, BepInEx checks it
    // https://github.com/BepInEx/BepInEx/blob/v5-lts/BepInEx.Preloader/Patching/AssemblyPatcher.cs#L67
    public static void Patch(AssemblyDefinition assembly)
    {
        if (assembly.Name.Name == "Assembly-CSharp-firstpass")
        {
            PatchES3(assembly);
            return;
        }

        Dictionary<string, Action<AssemblyDefinition, TypeDefinition>> workList = new()
        {
            { "AudioReverbPresets", (a, t) => AssemblyPatcherUtilities.AddMethod(a, t, "Awake") },
            // todo: detect if BrutalCompanyMinusExtra(Reborn) mod is installed
            // issue: https://github.com/DiFFoZ/LethalPerformance/issues/11
            //{ "animatedSun", (a, t) => AssemblyPatcherUtilities.RemoveMethod(a, t, "Update") },
        };

        foreach ((string typeName, Action<AssemblyDefinition, TypeDefinition> action) in workList)
        {
            var type = assembly.MainModule.GetType(typeName);
            if (type == null)
            {
                Logger.LogWarning("Failed to patch " + typeName);
                continue;
            }

            action(assembly, type);
        }
    }

    private static void PatchES3(AssemblyDefinition assembly)
    {
        var es3Type = assembly.MainModule.GetType("ES3");
        var es3SettingsType = assembly.MainModule.GetType("ES3Settings");

        PatchSave(assembly, es3Type, es3SettingsType);
        PatchLoad(assembly, es3Type, es3SettingsType);

        PatchSerialize(assembly, es3Type);

        var es3FileType = assembly.MainModule.GetType("ES3File");
        PatchFileSave(assembly, es3FileType, es3SettingsType);
    }

    private static void PatchFileSave(AssemblyDefinition assembly, TypeDefinition es3FileType, TypeDefinition es3SettingsType)
    {
        var saveMethod = es3FileType.Methods.FirstOrDefault(m => m.Name == "Save");

        var newSaveMethod = AddNonGenericMethod(assembly, es3FileType, null,
            "LethalPerformance_Save", MethodAttributes.Public);
        saveMethod.Body.Instructions.InsertRange(0, [
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Call, newSaveMethod),
        ]);
    }

    private static void PatchSave(AssemblyDefinition assembly, TypeDefinition es3Type, TypeReference es3SettingsType)
    {
        var origSaveMethod = es3Type.Methods.FirstOrDefault(m => m.Name == "Save"
                    && m.Parameters.Count == 3
                    && m.Parameters[2].ParameterType.Name == "ES3Settings"
                    && m.HasGenericParameters);

        if (origSaveMethod == null)
        {
            Logger.LogWarning("ES3.Save doesn't exists. Removed by other mod?");
            return;
        }

        var parameter = new ParameterDefinition("settings", ParameterAttributes.None, es3SettingsType);
        var newSaveMethod = AddNonGenericMethod(assembly, es3Type, parameter, "LethalPerformance_Save");

        origSaveMethod.Body.Instructions.InsertRange(0, [
            Instruction.Create(OpCodes.Ldarg, origSaveMethod.Parameters[^1]), // load ES3Settings
            Instruction.Create(OpCodes.Call, newSaveMethod)
            ]);
    }

    private static void PatchLoad(AssemblyDefinition assembly, TypeDefinition es3Type, TypeReference es3SettingsType)
    {
        var origLoadMethods = es3Type.Methods.Where(m =>
        {
            if (m.Name != "Load" || !m.ReturnType.IsGenericParameter)
            {
                return false;
            }

            if (m.Parameters.Count == 2
                && m.Parameters[0].ParameterType.Name == "String"
                && m.Parameters[1].ParameterType.Name == "ES3Settings")
            {
                return true;
            }
            else if (m.Parameters.Count == 3
                && m.Parameters[0].ParameterType.Name == "String"
                && m.Parameters[1].ParameterType.IsGenericParameter
                && m.Parameters[2].ParameterType.Name == "ES3Settings")
            {
                return true;
            }

            return false;
        }).ToList();

        if (origLoadMethods.Count != 2)
        {
            Logger.LogWarning("Detected ES3.Load mismatch of count");
            return;
        }

        var parameter = new ParameterDefinition("settings", ParameterAttributes.None, es3SettingsType);
        var newLoadMethod = AddNonGenericMethod(assembly, es3Type, parameter, "LethalPerformance_Load");

        foreach (var origLoadMethod in origLoadMethods)
        {
            origLoadMethod.Body.Instructions.InsertRange(0, [
                Instruction.Create(OpCodes.Ldarg, origLoadMethod.Parameters[^1]), // load ES3Settings
                Instruction.Create(OpCodes.Call, newLoadMethod)
                ]);
        }
    }

    private static void PatchSerialize(AssemblyDefinition assembly, TypeDefinition es3Type)
    {
        // due to some mods (LCModDataLib) that call ES3.Save<object>(string) method
        // makes ES3 to confuse and write type header to the value.
        // By patching Serialize to use GetType() instead of typeof() we are escaping this issue

        // If you're concern about null values, then it's ES3 error. ES3 cannot serialize object of null value,
        // it will throw null reference exception


        // Serialize<T>(T value, ES3Settings settings)
        var serializeMethod = es3Type.Methods.FirstOrDefault(m => m.Name == "Serialize"
            && m.Parameters.Count == 2);

        // Serialize(object value, ES3Type type, ES3Settings settings)
        var newSerializeMethod = es3Type.Methods.FirstOrDefault(m => m.Name == "Serialize"
            && m.Parameters.Count == 3);

        var es3TypeManagerGetOrCreateMethod = assembly.MainModule.GetType("ES3Internal.ES3TypeMgr")
            .Methods.FirstOrDefault(m => m.Name == "GetOrCreateES3Type");

        serializeMethod.Body.Instructions.Clear();
        serializeMethod.Body.Instructions.AddRange([
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Box, serializeMethod.Parameters[0].ParameterType), // T!! value to object
            Instruction.Create(OpCodes.Ldarga, serializeMethod.Parameters[0]), // load 'value'
            Instruction.Create(OpCodes.Constrained, serializeMethod.Parameters[0].ParameterType),
            Instruction.Create(OpCodes.Callvirt, assembly.MainModule.ImportReference(typeof(object).GetMethod("GetType"))),
            Instruction.Create(OpCodes.Ldc_I4_1), // "true"
            Instruction.Create(OpCodes.Call, es3TypeManagerGetOrCreateMethod),
            Instruction.Create(OpCodes.Ldarg_1),
            Instruction.Create(OpCodes.Call, newSerializeMethod),
            Instruction.Create(OpCodes.Ret),
            ]);
    }

    private static MethodDefinition AddNonGenericMethod(AssemblyDefinition assembly, TypeDefinition es3Type, ParameterDefinition? parameter,
        string name, MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.Static)
    {
        var saveMethod = new MethodDefinition(name,
            attributes,
            assembly.MainModule.TypeSystem.Void);

        if (parameter != null)
        {
            saveMethod.Parameters.Add(parameter);
        }

        AssemblyPatcherUtilities.StubMethod(saveMethod.Body.GetILProcessor());

        es3Type.Methods.Add(saveMethod);
        return saveMethod;
    }
}
