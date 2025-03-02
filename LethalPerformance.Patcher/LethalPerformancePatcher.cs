using System;
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
            { "DepositItemsDesk", (a, t) => AssemblyPatcherUtilities.AddMethod(a, t, "Awake") },
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

        var newSaveMethod = AddNonGenericMethod(assembly, es3Type, es3SettingsType, "LethalPerformance_Save");

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

        var newLoadMethod = AddNonGenericMethod(assembly, es3Type, es3SettingsType, "LethalPerformance_Load");

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

    private static MethodDefinition AddNonGenericMethod(AssemblyDefinition assembly, TypeDefinition es3Type, TypeReference es3SettingsType,
        string name)
    {
        var saveMethod = new MethodDefinition(name,
            MethodAttributes.Public | MethodAttributes.Static,
            assembly.MainModule.TypeSystem.Void);

        saveMethod.Parameters.Add(new ParameterDefinition("settings", ParameterAttributes.None, es3SettingsType));
        saveMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        es3Type.Methods.Add(saveMethod);
        return saveMethod;
    }
}
