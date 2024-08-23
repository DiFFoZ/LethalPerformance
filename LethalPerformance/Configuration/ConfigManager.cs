using System;
using BepInEx.Configuration;
using LethalPerformance.Utilities;
using UnityEngine.Rendering.HighDefinition;

namespace LethalPerformance.Configuration;
internal class ConfigManager
{
    private readonly ConfigFile m_Config;

    public ConfigManager(ConfigFile config)
    {
        m_Config = config;

        BindConfig();
    }

#nullable disable
    public ConfigEntry<CookieAtlasResolutionLimited> CookieAtlasResolution { get; private set; }

    public ConfigEntry<ReflectionProbeTextureCacheResolution> ReflectionProbeCacheResolution { get; private set; }

#nullable restore

    private void BindConfig()
    {
#pragma warning disable
#if ENABLE_PROFILER
        var force = true;

#else
        var force = false;
#endif
#pragma warning restore

        CookieAtlasResolution = BindRenderingConfig("Rendering", "Cookie atlas texture resolution", CookieAtlasResolutionLimited.CookieResolution1024,
            new("""
            Sets cookie light atlas texture resolution. By default 1024 is enough for vanilla, but some mods can use custom cookie texture, causing this log spam:
            "No more space in the 2D Cookie Texture Atlas. To solve this issue, increase the resolution of the cookie atlas in the HDRP settings".

            To fix it just increase the resolution of texture atlas.
            """, new AcceptableValueEnum<CookieAtlasResolutionLimited>()));

        ReflectionProbeCacheResolution = BindRenderingConfig("Rendering", "Reflection probe atlas texture resolution", ReflectionProbeTextureCacheResolution.Resolution2048x1024,
            new("""
            Sets reflection probe cache resolution. By default it's 16384x8192 causing high RAM usage (~1GB) even if vanilla game doesn't use them at all. But some mods may use, so it may cause this log spam:
            "No more space in Reflection Probe Atlas. To solve this issue, increase the size of the Reflection Probe Atlas in the HDRP settings".

            To fix it just increase the resolution of texture atlas.
            """, new AcceptableValueEnum<ReflectionProbeTextureCacheResolution>()));
    }

    private ConfigEntry<T> BindRenderingConfig<T>(string section, string key, T defaultValue, ConfigDescription? description)
    {
        var entry = m_Config.Bind(section, key, defaultValue, description);
        entry.SettingChanged += UpdateRenderingAsset;

        return entry;
    }

    private ConfigEntry<T> BindHarmonyConfig<T>(string section, string key, T defaultValue, string? description)
    {
        var configDescription = description == null ? null : new ConfigDescription(description);
        var entry = m_Config.Bind(section, key, defaultValue, configDescription);
        entry.SettingChanged += RepatchHarmony;

        return entry;
    }

    private static void UpdateRenderingAsset(object _, EventArgs __)
    {
        HDRenderPipelineAssetOptimizer.Initialize();
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
