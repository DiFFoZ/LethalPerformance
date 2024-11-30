using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace LethalPerformance.Patcher.Utilities;
internal static class AssemblyPatcherUtilities
{
    public static void RemoveMethod(AssemblyDefinition assembly, TypeDefinition type, string methodName)
    {
        foreach (var method in type.Methods)
        {
            if (method.Name == methodName)
            {
                type.Methods.Remove(method);
                return;
            }
        }
    }

    public static void AddMethod(AssemblyDefinition assembly, TypeDefinition type, string methodName)
    {
        if (type.Methods.Any(m => m.Name == methodName))
        {
            return;
        }

        var method = new MethodDefinition(methodName, MethodAttributes.Private,
            assembly.MainModule.TypeSystem.Void);

        var processor = method.Body.GetILProcessor();
        StubMethod(processor);

        type.Methods.Add(method);
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
