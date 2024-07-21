using System.Collections;
using System.IO;
using System.Reflection;
using HarmonyLib;
using LethalPerformance.API;

namespace LethalPerformance.Utilities;
[HarmonyPatch]
internal static class FileSystemWatcherDisabler
{
    private static readonly MethodInfo? s_StartDispatching;
    private static readonly FieldInfo? s_Watches;

    static FileSystemWatcherDisabler()
    {
        var type = typeof(FileSystemWatcher).Assembly.GetType("System.IO.DefaultWatcher", false);
        if (type == null)
        {
            return;
        }

        s_StartDispatching = type.GetMethod("StartDispatching", AccessTools.all);
        s_Watches = type.GetField("watches", AccessTools.all);
    }

    [InitializeOnAwake]
    private static void Initialize()
    {
        if (s_StartDispatching == null)
        {
            return;
        }

        s_Watches!.SetValue(null, new Hashtable());
    }

    [HarmonyPrepare]
    public static bool ShouldPatch()
    {
        return s_StartDispatching != null;
    }

    [HarmonyTargetMethod]
    public static MethodInfo? GetTargetPatch()
    {
        return s_StartDispatching!;
    }

    [HarmonyPrefix]
    private static bool DisableDefaultWatcher()
    {
        return false;
    }
}
