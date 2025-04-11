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

    public ConfigEntry<bool> CacheEntranceTeleports { get; private set; }

#nullable restore

    private void BindConfig()
    {
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

        CacheEntranceTeleports = m_Config.Bind("Caching", "Cache entrance teleport", true,
            """
            Should Entrance Teleport be cached for better performance. Note it causing blocked entrance on some custom interiors.
            You can find the tracking issue here: https://github.com/DiFFoZ/LethalPerformance/issues/15

            If you see this popup, it means that interior is misconfigurated and causing Lethal Performance to confuse about where the entrance teleport should be.

            Known mods that causing this issue:
            - [Tartarus](https://thunderstore.io/c/lethal-company/p/Teaisnt/Tartarus/)
            - [Sector Alpha](https://thunderstore.io/c/lethal-company/p/v0xx/SectorAlpha_Interior/)
            - [Hadal Laboratories](https://thunderstore.io/c/lethal-company/p/Tolian/Hadal_Laboratories/)
            - [MapImprovements](https://thunderstore.io/c/lethal-company/p/SpookyBuddy/MapImprovements/)

            To fix the issue you can set CacheEntranceTeleports to false or remove these mods above, until they fix the issue on their side.
            """);
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
