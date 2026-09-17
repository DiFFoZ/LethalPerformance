using System;
using System.Collections.Generic;
using LethalPerformance.Patcher.Utilities;
using Mono.Cecil;

namespace LethalPerformance.Patcher.AssemblyPatches;

internal static class Patch_AssemblyCSharp
{
    public static void Patch(AssemblyDefinition assembly)
    {
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
                LethalPerformancePatcher.Logger.LogWarning("Failed to patch " + typeName);
                continue;
            }

            action(assembly, type);
        }
    }
}