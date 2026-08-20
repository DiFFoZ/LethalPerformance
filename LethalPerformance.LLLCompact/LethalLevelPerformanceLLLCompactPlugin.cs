using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace LethalPerformance.LLLCompact;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(nameof(LethalPerformance), BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(LethalLevelLoader.Plugin.ModGUID, BepInDependency.DependencyFlags.HardDependency)]
public class LethalLevelPerformanceLLLCompactPlugin : BaseUnityPlugin
{
    public static LethalLevelPerformanceLLLCompactPlugin Instance { get; private set; } = null!;

    internal new ManualLogSource Logger { get; private set; } = null!;
    internal Harmony? Harmony { get; private set; }

    private void Awake()
    {
        Instance = this;
        Logger = base.Logger;

        Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        try
        {
            Harmony.PatchAll(typeof(LethalLevelPerformanceLLLCompactPlugin).Assembly);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }
}
