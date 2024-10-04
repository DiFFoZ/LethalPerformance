using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine.Rendering;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(VolumeComponent))]
internal static class Patch_VolumeComponent
{
    private static readonly MethodInfo s_ListAdd = typeof(List<VolumeParameter>).GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);

    private static readonly Dictionary<Type, Action<VolumeComponent, List<VolumeParameter>>?> s_FindParametersDelegates = new();

    [HarmonyPatch(nameof(VolumeComponent.FindParameters))]
    [HarmonyPrefix]
    private static bool FindParametersFast(object o, List<VolumeParameter> parameters)
    {
        var volumeComponent = (VolumeComponent)o;
        var componentType = o.GetType();

        if (!s_FindParametersDelegates.TryGetValue(componentType, out var action))
        {
            action = CreateFindParameters(componentType);
            s_FindParametersDelegates[componentType] = action;
        }

        if (action == null)
        {
            // return to the recursive reflection find
            return true;
        }

        action(volumeComponent, parameters);
        return false;
    }

    public static Action<VolumeComponent, List<VolumeParameter>>? CreateFindParameters(Type type)
    {
        var dm = new DynamicMethod("FindParametersFast", null,
            [typeof(VolumeComponent), typeof(List<VolumeParameter>)], type, true);
        var il = dm.GetILGenerator();

        var stack = new List<CodeInstruction>
        {
            new() { opCode = OpCodes.Ldarg_0 }
        };

        EmitAddItem(type, il, stack);

        il.Emit(OpCodes.Ret);

        try
        {
            return (Action<VolumeComponent, List<VolumeParameter>>)dm.CreateDelegate(typeof(Action<VolumeComponent, List<VolumeParameter>>));
        }
        catch (Exception ex)
        {
            LethalPerformancePlugin.Instance.Logger.LogError(ex);
        }
        return null;
    }

    private static void EmitAddItem(Type type, ILGenerator il, List<CodeInstruction> codeInstructions)
    {
        Label? label = default;
        // add null check if in stack more than 1 instructions
        if (codeInstructions.Count > 1)
        {
            foreach (var ci in codeInstructions)
            {
                ci.Emit(il);
            }

            label = il.DefineLabel();
            il.Emit(OpCodes.Brfalse_S, label.Value);
        }

        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .OrderBy(f => f.MetadataToken))
        {
            var fieldType = field.FieldType;

            if (fieldType.IsSubclassOf(typeof(VolumeParameter)))
            {
                il.Emit(OpCodes.Ldarg_1); // load list
                foreach (var ci in codeInstructions)
                {
                    ci.Emit(il);
                }
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Callvirt, s_ListAdd);

                continue;
            }

            if (fieldType.IsArray
                || !fieldType.IsClass
                || typeof(ICollection).IsAssignableFrom(fieldType) /* ignore fields that implements collection interface */)
            {
                continue;
            }

            codeInstructions.Add(new() { opCode = OpCodes.Ldfld, field = field });

            if (codeInstructions.Count > 30)
            {
                codeInstructions.ForEach(c => Console.WriteLine(c.field?.DeclaringType.Name + " " + c.field?.Name));
            }

            EmitAddItem(fieldType, il, codeInstructions);
            codeInstructions.RemoveAt(codeInstructions.Count - 1);
        }

        if (label == null)
        {
            return;
        }

        il.Emit(OpCodes.Nop);
        il.MarkLabel(label.Value);
    }

    private struct CodeInstruction
    {
        public OpCode opCode;
        public FieldInfo? field;

        public readonly void Emit(ILGenerator il)
        {
            if (field == null)
            {
                il.Emit(opCode);
                return;
            }

            il.Emit(opCode, field);
        }
    }
}
