using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Unity.Burst.LowLevel;

namespace LethalPerformance;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
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

        LoadGameBurstLib();

        m_Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        m_Harmony.PatchAll(typeof(LethalPerformancePlugin).Assembly);
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
