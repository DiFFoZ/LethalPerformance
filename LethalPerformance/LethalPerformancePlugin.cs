using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Logging;
using DunGen;
using HarmonyLib;
using LethalPerformance.Configuration;
using LethalPerformance.Dungen;
using LethalPerformance.Patcher;
using LethalPerformance.Patcher.API;
using LethalPerformance.Patches.Mods;
using LethalPerformance.Utilities;
using Unity.Burst.LowLevel;
using UnityEngine;

namespace LethalPerformance;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class LethalPerformancePlugin : BaseUnityPlugin
{
    public static LethalPerformancePlugin Instance { get; private set; } = null!;

    internal new ManualLogSource Logger { get; private set; } = null!;
    internal string WorkingDirectory { get; private set; } = null!;
    internal ConfigManager Configuration { get; private set; } = null!;
    internal Harmony? Harmony { get; private set; }

    internal ES3SaverTask ES3SaverTask { get; } = new();

    protected void Awake()
    {
        Instance = this;
        Logger = base.Logger;
        WorkingDirectory = new FileInfo(Info.Location).DirectoryName;
        Configuration = new(base.Config);

#if ENABLE_PROFILER
        // disable overhead of stack trace in dev build
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Assert, StackTraceLogType.None);

        Debug.developerConsoleEnabled = false;
        Debug.developerConsoleVisible = false;
#endif

        LoadGameBurstLib();
        InitializeHarmony();
        CallInitializeOnAwake();
        InitializeSaveScheduler();

        LethalPerformancePatcher.OnModsLoaded += LethalPerformancePatcher_OnModsLoaded;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private void LethalPerformancePatcher_OnModsLoaded()
    {
        var harmony = Harmony!;

        if (Dependencies.IsModLoaded(Dependencies.LethalLevelLoader))
        {
            harmony.Unpatch(AccessTools.Method(typeof(DoorwayPairFinder), nameof(DoorwayPairFinder.GetDoorwayPairs)),
                HarmonyPatchType.Prefix, Dependencies.LethalLevelLoader);

            Patch_Dungeon.PatchAll();
            harmony.PatchAll(typeof(Patch_LethalLevelLoader));
        }

        if (Dependencies.IsModLoaded(Dependencies.DungenPlus))
        {
            harmony.PatchAll(typeof(Patch_DungeonPlus));
        }

        if (Dependencies.IsModLoaded(Dependencies.MoreCompany))
        {
            harmony.PatchAll(typeof(Patch_MoreCompany));
        }

        if (Dependencies.IsModLoaded(Dependencies.MoreSuits))
        {
            harmony.PatchAll(typeof(Patch_MoreSuits));
        }
    }

    private void InitializeSaveScheduler()
    {
        var go = new GameObject("Lethal Performance Configuration Scheduler", typeof(SaveScheduleComponent));
        go.hideFlags = HideFlags.HideAndDontSave;

        DontDestroyOnLoad(go);
    }

    private void InitializeHarmony()
    {
        // disables bepinex harmony logger filter temporarily
        var oldFilter = HarmonyLib.Tools.Logger.ChannelFilter;
        if ((oldFilter & HarmonyLib.Tools.Logger.LogChannel.IL) == 0)
        {
            // replace log filter only if IL log channel is not enabled
            HarmonyLib.Tools.Logger.ChannelFilter = HarmonyLib.Tools.Logger.LogChannel.None;
        }

        Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        try
        {
            Harmony.PatchAll(typeof(LethalPerformancePlugin).Assembly);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }

        HarmonyLib.Tools.Logger.ChannelFilter = oldFilter;
    }

    private void CallInitializeOnAwake()
    {
        var types = Array.Empty<Type>();
        try
        {
            types = typeof(LethalPerformancePlugin).Assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException tle)
        {
            types = [.. tle.Types.Where(t => t != null)];
        }
       
        foreach (var method in types
            .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(m => m.GetCustomAttribute<InitializeOnAwakeAttribute>() != null))
        {
            try
            {
                Logger.LogInfo($"Initializating {method.FullDescription()}");
                method.Invoke(null, null);
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to initialize\n" + ex);
            }
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
