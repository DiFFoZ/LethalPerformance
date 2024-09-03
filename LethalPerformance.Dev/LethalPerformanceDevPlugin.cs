using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using LethalPerformance.Dev.Configuration;
using LethalPerformance.Patcher.API;

namespace LethalPerformance.Dev;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class LethalPerformanceDevPlugin : BaseUnityPlugin
{
    public static LethalPerformanceDevPlugin Instance { get; private set; } = null!;

    public new ConfigManager Config { get; private set; } = null!;

    private Harmony? m_Harmony;

    private void Awake()
    {
        Instance = this;
        Config = new(base.Config);

        m_Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        m_Harmony.PatchAll(typeof(LethalPerformanceDevPlugin).Assembly);

        CallInitializeOnAwake();
    }

    private void CallInitializeOnAwake()
    {
        foreach (var method in typeof(LethalPerformanceDevPlugin)
            .Assembly
            .GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(m => m.GetCustomAttribute<InitializeOnAwakeAttribute>() != null))
        {
            method.Invoke(null, null);
        }
    }
}
