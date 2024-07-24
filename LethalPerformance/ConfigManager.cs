using System;
using BepInEx.Configuration;

namespace LethalPerformance;
internal class ConfigManager
{
    private readonly ConfigFile m_Config;

    public ConfigManager(ConfigFile config)
    {
        m_Config = config;

        BindConfig();
    }

#nullable disable

    public ConfigEntry<bool> PatchHDRenderPipeline { get; private set; }

    public ConfigEntry<bool> CompressSuitsTextures { get; private set; }

#nullable restore

    private void BindConfig()
    {
#if ENABLE_PROFILER
        var force = true;
#else
        var force = false;
#endif

        PatchHDRenderPipeline = BindHarmonyConfig("Unsafe.Rendering", "Remove useless calls from HDRenderPipeline", force || false,
            """
            Remove useless method calls in rendering to improve performance.
            May cause graphical issues, if you noticed them, disable this option.
            """);

        CompressSuitsTextures = BindHarmonyConfig("Experimental.Mods", "Compress custom suits textures", force || false,
            """
            Compress custom suits from MoreSuits mod to reduce usage of VRAM.
            """);
    }

    private ConfigEntry<T> BindHarmonyConfig<T>(string section, string key, T defaultValue, string? description)
    {
        var configDescription = description == null ? null : new ConfigDescription(description);
        var entry = m_Config.Bind(section, key, defaultValue, configDescription);
        entry.SettingChanged += RepatchHarmony;

        return entry;
    }

    private static void RepatchHarmony(object _, EventArgs __)
    {
        LethalPerformancePlugin.Instance.Logger.LogInfo("Config option of Harmony got changed, repatching...");

        var harmony = LethalPerformancePlugin.Instance.Harmony;
        if (harmony == null)
        {
            return;
        }

        // todo: add our own HarmonyCategory to HarmonyX

        try
        {
            harmony.UnpatchSelf();
            harmony.PatchAll(typeof(ConfigManager).Assembly);
        }
        catch (Exception ex)
        {
            LethalPerformancePlugin.Instance.Logger.LogError("Failed to repatch with Harmony\n" + ex);
        }
    }
}
