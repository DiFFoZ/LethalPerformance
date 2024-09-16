using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using static System.Reflection.Emit.OpCodes;

namespace LethalPerformance.Patcher.Patches;

[HarmonyPatch(typeof(Chainloader))]
internal static class Patch_Chainloader
{
    // todo: cleanup after start up?
#pragma warning disable IDE0044 // Add readonly modifier
    private static Dictionary<string, PluginInfo>? s_PluginsToLoad;
#pragma warning restore IDE0044 // Add readonly modifier

    internal static bool IsModWillBeLoaded(string guid)
    {
        return s_PluginsToLoad?.ContainsKey(guid) == true;
    }

    [HarmonyPatch(nameof(Chainloader.Initialize))]
    [HarmonyPostfix]
    private static void Initialize()
    {
        // cannot patch Chainloader.Start immediately because of Unity usage inside of method, waiting when Initialize called to patch
        try
        {
            LethalPerformancePatcher.Harmony!.Patch(MethodOf(Chainloader.Start),
                transpiler: new(MethodOf(SaveListOfPluginsTranspiler)));
        }
        catch (Exception e)
        {
            LethalPerformancePatcher.Logger.LogWarning(e);
        }
    }

    private static MethodInfo MethodOf(Delegate @delegate) => @delegate.Method;

    private static IEnumerable<CodeInstruction> SaveListOfPluginsTranspiler(IEnumerable<CodeInstruction> codeInstructions)
    {
        var matcher = new CodeMatcher(codeInstructions);

        var localDict = matcher
            .SearchForward(c => c.operand is FieldInfo fi && fi.FieldType == typeof(Dictionary<string, PluginInfo>))
            .Operand;

        matcher
            .Start()
            .MatchForward(true, // find where empty string array initializes
            [
            new(Ldloc_0),
            new(Ldc_I4_0),
            new(Newarr, typeof(string)),
            new(Stfld),
            ])
            .ThrowIfInvalid("Failed to get injection point")
            .Insert([ // insert loading local** and storing it here
                new(Ldloc_0),
                new(Ldfld, localDict),
                new(Stsfld, typeof(Patch_Chainloader).GetField(nameof(s_PluginsToLoad), AccessTools.all))
            ]);

        return matcher.InstructionEnumeration();
    }
}
