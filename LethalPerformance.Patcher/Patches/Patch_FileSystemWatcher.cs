using System.Collections;
using System.IO;
using System.Reflection;
using HarmonyLib;

namespace LethalPerformance.Patcher.Patches;
[HarmonyPatch]
internal class Patch_FileSystemWatcher
{
    private static readonly MethodInfo? s_StartDispatching;
    private static readonly FieldInfo? s_Watches;

    static Patch_FileSystemWatcher()
    {
        var type = typeof(FileSystemWatcher).Assembly.GetType("System.IO.DefaultWatcher", false);
        if (type == null)
        {
            return;
        }

        s_StartDispatching = type.GetMethod("StartDispatching", AccessTools.all);
        s_Watches = type.GetField("watches", AccessTools.all);

        s_Watches?.SetValue(null, new Hashtable());
    }

    [HarmonyPrepare]
    private static bool ShouldPatch()
    {
        return s_StartDispatching != null;
    }

    [HarmonyTargetMethod]
    private static MethodInfo? GetTargetPatch()
    {
        return s_StartDispatching!;
    }

    [HarmonyPrefix]
    private static bool DisableDefaultWatcher()
    {
        return false;
    }
}
