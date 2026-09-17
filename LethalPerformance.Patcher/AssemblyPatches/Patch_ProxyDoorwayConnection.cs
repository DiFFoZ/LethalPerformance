using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace LethalPerformance.Patcher.AssemblyPatches;

internal static class Patch_ProxyDoorwayConnection
{
    public static void Patch(AssemblyDefinition assembly)
    {
        var module = assembly.MainModule;
        var type = module.GetType("DunGen.ProxyDoorwayConnection");
        if (type == null)
        {
            LethalPerformancePatcher.Logger.LogWarning("Failed to patch ProxyDoorwayConnection");
            return;
        }

        var openEquatableDef = module.ImportReference(typeof(IEquatable<>));
        if (type.Interfaces.Any(i => i.InterfaceType.GetElementType().FullName == openEquatableDef.FullName))
        {
            return;
        }

        var getA = type.Methods.FirstOrDefault(m => m.Name == "get_A");
        var getB = type.Methods.FirstOrDefault(m => m.Name == "get_B");
        if (getA == null || getB == null)
        {
            LethalPerformancePatcher.Logger.LogWarning("Failed to patch ProxyDoorwayConnection");
            return;
        }

        var iequatable = new GenericInstanceType(openEquatableDef);
        iequatable.GenericArguments.Add(type);
        type.Interfaces.Add(new InterfaceImplementation(iequatable));

        type.Methods.Add(CreateEqualsMethod(module, type, getA, getB));
    }

    private static MethodDefinition CreateEqualsMethod(ModuleDefinition module, TypeDefinition type,
        MethodDefinition getA, MethodDefinition getB)
    {
        var method = new MethodDefinition(
            "Equals",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
            module.TypeSystem.Boolean);

        var other = new ParameterDefinition("other", ParameterAttributes.None, type);
        method.Parameters.Add(other);

        var il = method.Body.GetILProcessor();
        var notEqual = il.Create(OpCodes.Ldc_I4_0);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, getA);
        il.Emit(OpCodes.Ldarga, other);
        il.Emit(OpCodes.Call, getA);
        il.Emit(OpCodes.Bne_Un, notEqual);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, getB);
        il.Emit(OpCodes.Ldarga, other);
        il.Emit(OpCodes.Call, getB);
        il.Emit(OpCodes.Ceq);
        il.Emit(OpCodes.Ret);

        il.Append(notEqual);
        il.Emit(OpCodes.Ret);

        return method;
    }
}