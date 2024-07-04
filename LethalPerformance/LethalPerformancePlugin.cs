using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalPerformance.API;
using Unity.Burst.LowLevel;
using UnityEngine;

namespace LethalPerformance;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("LethalPerformance.Unity", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("com.adibtw.loadstone", BepInDependency.DependencyFlags.SoftDependency)] // make loadstone to patch firstly
public class LethalPerformancePlugin : BaseUnityPlugin
{
    public static LethalPerformancePlugin Instance { get; private set; } = null!;

    internal new ManualLogSource Logger { get; private set; } = null!;
    internal string WorkingDirectory { get; private set; } = null!;
    internal new ConfigManager Config { get; private set; } = null!;

    private Harmony? m_Harmony;

    private void Awake()
    {
        Instance = this;
        Logger = base.Logger;
        WorkingDirectory = new FileInfo(Info.Location).DirectoryName;
        Config = new(base.Config);

#if ENABLE_PROFILER
        // disable overhead of stack trace in dev build
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
#endif

        LoadGameBurstLib();
        CallInitializeOnAwake();

        // disables bepinex harmony logger filter temporarily
        var oldFilter = HarmonyLib.Tools.Logger.ChannelFilter;
        if ((oldFilter & HarmonyLib.Tools.Logger.LogChannel.IL) == 0)
        {
            // replace log filter only if IL log channel is not enabled
            HarmonyLib.Tools.Logger.ChannelFilter = HarmonyLib.Tools.Logger.LogChannel.None;
        }

        m_Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        m_Harmony.PatchAll(typeof(LethalPerformancePlugin).Assembly);

        HarmonyLib.Tools.Logger.ChannelFilter = oldFilter;
    }

    private void CallInitializeOnAwake()
    {
        foreach (var method in typeof(LethalPerformancePlugin)
            .Assembly
            .GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(m => m.GetCustomAttribute<InitializeOnAwakeAttribute>() != null))
        {
            method.Invoke(null, null);
            Logger.LogInfo($"Initialized {method.FullDescription()}");
        }
    }

    private void LoadGameBurstLib()
    {
        // Unity supports loading additional burst libs at runtime
        // see https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/modding-support.html

        const string c_LibName = "lib_burst_generated.data";

        var burstLibPath = Path.Combine(WorkingDirectory, c_LibName);
        if (!File.Exists(burstLibPath))
        {
            Logger.LogFatal($"Failed to find \"{c_LibName}\"");
            return;
        }

        var isLoaded = BurstCompilerService.LoadBurstLibrary(burstLibPath);
        if (!isLoaded)
        {
            Logger.LogFatal("Failed to load burst library. Probably machine architecture is not x64 or CPU doesn't support AVX2 and SSE2 instructions");
        }
    }
}
